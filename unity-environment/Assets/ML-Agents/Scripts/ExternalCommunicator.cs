using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using UnityEngine;

using Newtonsoft.Json;

/// <summary>
/// Helper class to parse and represent the command-line arguments for this
/// external communicator. The arguments can appear is any order as long
/// as the flag value immediately follows the flag name.
/// </summary>
class CommunicatorArgs
{
    /// Default server address to use.
    const string DEFAULT_SERVER = "localhost";

    /// Server flag used within command-line arguments.
    const string SERVER_FLAG = "--server";

    /// Port flag used within command-line arguments.
    const string PORT_FLAG = "--port";

    /// Random seed flag used within command-line arguments.
    const string SEED_FLAG = "--seed";

    /// Socket server address.
    public readonly string server;

    /// Socket port number.
    public readonly int port;

    /// Random seed value.
    public readonly int seed;

    /// Initializes a new instance of the <see cref="CommunicatorArgs"/> class.
    /// Server is optional (if not provided, we revert to default one), but
    /// port and seed are required.
    CommunicatorArgs(string server, int port, int seed)
    {
        this.server = DEFAULT_SERVER;
        if (!string.IsNullOrEmpty(server))
        {
            this.server = server;
        }
        this.port = port;
        this.seed = seed;
    }

    /// Parses the provided command-line arguments array and returns a 
    /// <see cref="CommunicatorArgs"/> object that contains the arguments.
    /// In case of any parsing errors, a runtime exception is thrown.
    public static CommunicatorArgs Parse(string[] args)
    {
        string server = null;
        int port = -1;
        int seed = -1;

        // Only iterate to second-last element to ensure no index overflow.
        int i = 0;
        while (i < args.Length - 1)
        {
            switch (args[i])
            {
                case PORT_FLAG:
                    port = int.Parse(args[i + 1]);
                    i++;
                    break;
                case SERVER_FLAG:
                    server = args[i + 1];
                    i++;
                    break;
                case SEED_FLAG:
                    seed = int.Parse(args[i + 1]);
                    i++;
                    break;
            }
            i++;
        }

        return new CommunicatorArgs(server, port, seed);
    }
}

/// <summary>
/// Implements the Communicator interface by using sockets to communicate
/// with the Python API. Currently, this class is not minimal, as it implements
/// other public facing methods that are not defined as part of the interface.
/// </summary>
public class ExternalCommunicator : Communicator
{
    /// Size of the message buffer, <see cref="messageBuffer"/>.
    const int MESSAGE_BUFFER_SIZE = 12000;

    /// Size of the length buffer. <see cref="lengthBuffer"/>.
    const int LENGTH_BUFFER_SIZE = 4;

    /// Contains the log filename.
    const string LOG_FILENAME = "unity-environment.log";

    /// API version dictates the format of the messages sent, so any changes
    /// in the message format dictates a new API version.
    const string API_VERSION = "API-2";

    /// Default number of agents - used to initialize data structure capacity.
    const int DEFAULT_NUM_AGENTS = 32;

    /// Default number of observations per agent - used to initialize data
    /// structure capacity.
    const int DEFAULT_NUM_OBSERVATIONS = 32;

    /// Contains the command-line arguments passed to communicator.
    CommunicatorArgs args;

    /// The Academy for the learning environment that is communicating with
    /// the Python API.
    Academy academy;

    /// List of brain GameObjects in the environment that are children of the
    /// Academy GameObject.
    List<Brain> brains;

    /// List of Agent (ids) attached to each Brain. Dictionary maps brain name
    /// (as specified in Unity environment GameObject) to list of agent IDs
    /// (an agent ID is a unique identifier each agent GameObject receives
    /// at initialization).
    Dictionary<string, List<int>> brainAgents;

    /// Maps Brain name to whether it has sent its state (information about all
    /// its Agent objects attached to the brain). The value is flipped to True
    /// in <see cref="SendBrainInfo"/> as that is where all the Brain info is
    /// sent. Size of dictionary corresponds to the number of brains defined
    /// in the Unity environment.
    Dictionary<string, bool> hasSentState;

    /// Maps Brain name to a dictionary which in turn maps each Agent ID to
    /// an array of actions. Size of outer dictionary corresponds to the number
    /// of brains defined in the Unity environment. Size of inner dictionary
    /// corresponds to the number of agents attached to the corresponding brain.
    /// Updated following <see cref="ReceiveActions"/>. 
    Dictionary<string, Dictionary<int, float[]>> storedActions;

    /// Maps Brain name to a dictionary which in turn maps each Agent ID to
    /// an array of memories. Size of outer dictionary corresponds to the
    /// number of brains defined in the Unity environment. Size of inner
    /// dictionary corresponds to the number of agents attached to the
    /// corresponding brain. Updated following <see cref="ReceiveActions"/>.
    Dictionary<string, Dictionary<int, float[]>> storedMemories;

    /// Maps Brain name to a dictionary which in turn maps each Agent ID to an
    /// array of value estimates. Size of outer dictionary corresponds to the
    /// number of brains defined in the Unity environment. Size of inner 
    /// dictionary corresponds to the number of agents attached to the 
    /// corresponding brain. Updated following <see cref="ReceiveActions"/>.
    Dictionary<string, Dictionary<int, float>> storedValues;

    /// Socket used to send and receive messages with Python API.
    Socket socket;

    /// Re-usable buffer to store messages received from Python API.
    byte[] messageBuffer;

    /// Re-usable buffer to store length of messages forthcoming from Python
    /// API. Before we receive a large message from the Python API, it first
    /// sends length information. This buffer stores that length information.
    byte[] lengthBuffer;

    /// Path to the log file that is used by the communicator. It is passed to
    /// the Python API as part of the first message so it can load and display
    /// logging information.
    string logPath;

    /// Format of the Step message which is sent to the Python API.
    [System.Serializable]
    public struct StepMessage
    {
        public string brain_name;
        public List<int> agents;
        public List<float> states;
        public List<float> rewards;
        public List<float> actions;
        public List<float> memories;
        public List<bool> dones;
        public List<bool> maxes;
    }

    /// Buffer object that is re-used to store StepMessages (container for
    /// messages sent from Unity to Python).
    StepMessage stepMessageBuffer;

    /// Format of the Agent message which is sent from the Python API to Unity.
    struct AgentMessage
    {
        public Dictionary<string, List<float>> action { get; set; }
        public Dictionary<string, List<float>> memory { get; set; }
        public Dictionary<string, List<float>> value { get; set; }
    }

    /// Format of the ResetParameters message. Contains both the reset
    /// parameters and a boolean train model flag which indicates whether
    /// the learning environment mode is training (true) or inference (false).
    struct ResetParametersMessage
    {
        public Dictionary<string, float> parameters { get; set; }
        public bool train_model { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalCommunicator"/>
    /// class. Specifically, initializes the internal data structures.
    /// </summary>
    /// <param name="academy">
    /// Academy that this communicator handles communication for.
    /// </param>
    public ExternalCommunicator(Academy academy)
    {
        this.academy = academy;
        brains = new List<Brain>();
        brainAgents = new Dictionary<string, List<int>>();

        hasSentState = new Dictionary<string, bool>();

        storedActions = new Dictionary<string, Dictionary<int, float[]>>();
        storedMemories = new Dictionary<string, Dictionary<int, float[]>>();
        storedValues = new Dictionary<string, Dictionary<int, float>>();

        messageBuffer = new byte[MESSAGE_BUFFER_SIZE];
        lengthBuffer = new byte[LENGTH_BUFFER_SIZE];

        // Capacity for collection objects is intended to avoid initial
        // repeated resizing.
        stepMessageBuffer = new StepMessage
        {
            agents =
                new List<int>(DEFAULT_NUM_AGENTS),
            states =
                new List<float>(DEFAULT_NUM_AGENTS * DEFAULT_NUM_OBSERVATIONS),
            rewards =
                new List<float>(DEFAULT_NUM_AGENTS),
            actions =
                new List<float>(DEFAULT_NUM_AGENTS * DEFAULT_NUM_OBSERVATIONS),
            memories =
                new List<float>(DEFAULT_NUM_AGENTS * DEFAULT_NUM_OBSERVATIONS),
            maxes =
                new List<bool>(DEFAULT_NUM_AGENTS),
            dones =
                new List<bool>(DEFAULT_NUM_AGENTS)
        };
    }

    /// <summary> <inheritdoc/> </summary>
    /// <param name="brain">Brain to register.</param>
    public void SubscribeBrain(Brain brain)
    {
        brains.Add(brain);
        hasSentState[brain.gameObject.name] = false;
    }

    /// <summary> <inheritdoc/> </summary>
    /// <remarks>
    /// Attempts a handshake with external API. Note that in the current
    /// implementation, only the validity of the command-line arguments is
    /// verified and no actualy handshake is performed. Furthermore, this call
    /// is stateful since the command-line arguments are parsed and stored
    /// within an instance variable.
    /// </remarks>
    /// <returns>
    /// <c>true</c>, if handshake was successful; <c>false</c> otherwise.
    /// </returns>
    public bool CommunicatorHandShake()
    {
        try
        {
            args = CommunicatorArgs.Parse(
                System.Environment.GetCommandLineArgs());
        }
        catch
        {
            return false;
        }
        Random.InitState(args.seed);
        return true;
    }

    /// <summary> <inheritdoc/> </summary>
    /// <remarks>
    /// Initializes the socket and sends the first message to the Python API.
    /// </remarks>
    public void InitializeCommunicator()
    {
        // Attach logger callback to the logMessageReceived event
        Application.logMessageReceived += HandleLog;

        // Initialize log file.
        logPath =
            Path.GetFullPath(".") + Path.DirectorySeparatorChar + LOG_FILENAME;
        StreamWriter logWriter = new StreamWriter(logPath, false);
        logWriter.WriteLine(System.DateTime.Now.ToString());
        logWriter.WriteLine(" ");
        logWriter.Close();

        // Create a TCP/IP socket connection.
        socket = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(args.server, args.port);

        // Initialize and populate the academy parameters.
        var academyParams = new AcademyParameters
        {
            AcademyName = academy.gameObject.name,
            resetParameters = academy.resetParameters,
            brainParameters = new List<BrainParameters>(),
            brainNames = new List<string>(),
            externalBrainNames = new List<string>(),
            apiNumber = API_VERSION,
            logPath = logPath
        };
        foreach (Brain brain in brains)
        {
            academyParams.brainParameters.Add(brain.brainParameters);
            academyParams.brainNames.Add(brain.gameObject.name);
            if (brain.brainType == BrainType.External)
            {
                academyParams.externalBrainNames.Add(brain.gameObject.name);
            }
        }

        // Send academy parameters.
        SendParameters(academyParams);
    }

    /// Callback method attached to the logMessageReceived event. Simply prints
    /// the log message details to file.
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        StreamWriter logWriter = new StreamWriter(logPath, true);
        logWriter.WriteLine(type.ToString());
        logWriter.WriteLine(logString);
        logWriter.WriteLine(stackTrace);
        logWriter.Close();
    }

    /// <summary> <inheritdoc/> </summary>
    /// <remarks>
    /// If the command received is not recognized, it defaults to QUIT.
    /// </remarks>
    /// <returns>The command received from the socket.</returns>
    public ExternalCommand ReceiveCommand()
    {
        string message = ReceiveShortMessage();
        switch (message)
        {
            case "STEP":
                return ExternalCommand.STEP;
            case "RESET":
                return ExternalCommand.RESET;
            case "QUIT":
                return ExternalCommand.QUIT;
            default:
                return ExternalCommand.QUIT;
        }
    }

    /// <summary> <inheritdoc/> </summary>
    public Dictionary<string, float> ReceiveResetParameters()
    {
        SendShortMessage("CONFIG_REQUEST");
        string message = ReceiveShortMessage();
        var resetParams =
            JsonConvert.DeserializeObject<ResetParametersMessage>(message);
        academy.SetInference(!resetParams.train_model);
        return resetParams.parameters;
    }

    /// Sends Academy parameters to Python API.
    void SendParameters(AcademyParameters academyParameters)
    {
        SendShortMessage(
            JsonConvert.SerializeObject(
                academyParameters, Formatting.Indented));
    }

    /// Receives a short message from the Python API.
    string ReceiveShortMessage()
    {
        int location = socket.Receive(messageBuffer);
        return Encoding.ASCII.GetString(messageBuffer, 0, location);
    }

    /// Receives a long message from the Python API by piecing multiple chunks.
    string ReceiveLongMessage()
    {
        socket.Receive(lengthBuffer);
        int totalLength = System.BitConverter.ToInt32(lengthBuffer, 0);
        int location = 0;
        StringBuilder message = new StringBuilder();
        while (location != totalLength)
        {
            int fragment = socket.Receive(messageBuffer);
            location += fragment;
            message.Append(Encoding.ASCII.GetString(messageBuffer, 0, fragment));
        }
        return message.ToString();
    }

    /// Sends a short message to the Python API. Performs the necessary
    /// conversion from string to byte array.
    void SendShortMessage(string message)
    {
        SendShortMessage(Encoding.ASCII.GetBytes(message));
    }

    /// Sends a short message to the Python API.
    void SendShortMessage(byte[] message)
    {
        socket.Send(message);
    }

    /// Sends a long message to the Python API. Performs the necessary
    /// conversion from string to byte array in addition to prepending
    /// the message length.
    void SendLongMessage(string message)
    {
        SendLongMessage(Encoding.ASCII.GetBytes(message));
    }

    /// Sends a long message to the Python API. Prepends the message length.
    void SendLongMessage(byte[] message)
    {
        socket.Send(PrependLength(message));
    }

    /// Ends connection and closes environment.
    void OnApplicationQuit()
    {
        socket.Close();
        socket.Shutdown(SocketShutdown.Both);
    }

    /// Converts texture object to a byte array to enable sending to the
    /// Python API.
    static byte[] TextureToByteArray(Texture2D tex)
    {
        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        Resources.UnloadUnusedAssets();
        return bytes;
    }

    /// Creates a new array object that contains the contents of the input
    /// array, except that the first 4 elements of the new array represent
    /// the length of the original array.
    static byte[] PrependLength(byte[] input)
    {
        byte[] newArray = new byte[input.Length + 4];
        input.CopyTo(newArray, 4);
        System.BitConverter.GetBytes(input.Length).CopyTo(newArray, 0);
        return newArray;
    }

    /// <summary>
    /// Collects the information from the brains agents and sends it to the
    /// Python API, in addition to a boolean flag if the Academy is done.
    /// Sends a single <see cref="StepMessage"/> message (which encompasses 
    /// information from all the agents) and then one image for every
    /// camera-agent combination. Following each sent message is a recieve
    /// call that is not parsed. Lastly, a boolean flag is sent to indicate
    /// if the academy is done.
    /// </summary>
    /// <param name="brain">Brain.</param>
    public void SendBrainInfo(Brain brain)
    {
        var brainName = brain.gameObject.name;
        brainAgents[brainName] = new List<int>(brain.agents.Keys);
        brain.CollectEverything();

        stepMessageBuffer.brain_name = brainName;
        stepMessageBuffer.agents = brainAgents[brainName];
        stepMessageBuffer.states.Clear();
        stepMessageBuffer.rewards.Clear();
        stepMessageBuffer.memories.Clear();
        stepMessageBuffer.dones.Clear();
        stepMessageBuffer.maxes.Clear();
        stepMessageBuffer.actions.Clear();

        foreach (int id in brainAgents[brainName])
        {
            stepMessageBuffer.states.AddRange(
                brain.currentStates[id]);
            stepMessageBuffer.rewards.Add(
                brain.currentRewards[id]);
            stepMessageBuffer.memories.AddRange(
                brain.currentMemories[id].ToList());
            stepMessageBuffer.dones.Add(
                brain.currentDones[id]);
            stepMessageBuffer.maxes.Add(
                brain.currentMaxes[id]);
            stepMessageBuffer.actions.AddRange(
                brain.currentActions[id].ToList());
        }

        SendLongMessage(JsonUtility.ToJson(stepMessageBuffer));
        ReceiveShortMessage();
        int i = 0;
        foreach (Resolution res in brain.brainParameters.cameraResolutions)
        {
            foreach (int agentId in brainAgents[brainName])
            {
                SendLongMessage(
                    TextureToByteArray(
                        brain.ObservationToTex(
                            brain.currentCameras[agentId][i],
                            res.width,
                            res.height)));
                ReceiveShortMessage();
            }
            i++;
        }

        hasSentState[brainName] = true;

        if (hasSentState.Values.All(x => x))
        {
            // If all the brains listed have sent their state.
            SendShortMessage(academy.IsDone() ? "True" : "False");
            ResetSentState();
        }

    }

    /// <summary>
    /// Sets the sent state to false for all subscribed Brain objects.
    /// </summary>
    void ResetSentState()
    {
        List<string> brainNames = hasSentState.Keys.ToList();
        foreach (string brainName in brainNames)
        {
            hasSentState[brainName] = false;
        }
    }

    /// <summary>
    /// Listens for actions, memories, and values from the Python API.
    /// Sends a STEPPING message to the Python API and receives an
    /// <see cref="AgentMessage"/> message which is folded into
    /// <see cref="storedActions"/>, <see cref="storedMemories"/>, and
    /// <see cref="storedValues"/>.
    /// </summary>
    public void ReceiveActions()
    {
        SendShortMessage("STEPPING");
        string message = ReceiveLongMessage();
        var agentMessage =
            JsonConvert.DeserializeObject<AgentMessage>(message);

        foreach (Brain brain in brains)
        {
            if (brain.brainType == BrainType.External)
            {
                var brainName = brain.gameObject.name;

                storedActions[brainName].Clear();
                storedMemories[brainName].Clear();
                storedValues[brainName].Clear();

                int numAgents = brainAgents[brainName].Count;
                for (int agentId = 0; agentId < numAgents; agentId++)
                {
                    // Update action depending on whether action space is
                    // discrete or continuous.
                    if (brain.brainParameters.actionSpaceType ==
                        StateType.continuous)
                    {
                        storedActions[brainName].Add(
                            brainAgents[brainName][agentId],
                            agentMessage.action[brainName]
                                .GetRange(
                                    agentId * brain.brainParameters.actionSize,
                                    brain.brainParameters.actionSize)
                                .ToArray());
                    }
                    else
                    {
                        storedActions[brainName].Add(
                            brainAgents[brainName][agentId],
                            agentMessage.action[brainName]
                                .GetRange(agentId, 1)
                            .ToArray());
                    }

                    // Update memories (entire array for each agent).
                    storedMemories[brainName].Add(
                        brainAgents[brainName][agentId],
                        agentMessage.memory[brainName]
                            .GetRange(agentId * brain.brainParameters.memorySize,
                                      brain.brainParameters.memorySize)
                            .ToArray());

                    // Update value estimates (one value for each agent)
                    storedValues[brainName].Add(
                        brainAgents[brainName][agentId],
                        agentMessage.value[brainName][agentId]);
                }
            }
        }
    }

    /// <summary>
    /// Returns the actions corresponding to the provided Brain that were
    /// received from the Python API during the last call to
    /// <see cref="ReceiveActions"></see>
    /// </summary>
    /// <returns>The actions for all agents attached to the brain,
    /// indexed by the agent ids.</returns>
    /// <param name="brainName">Brain name.</param>
    public Dictionary<int, float[]> GetDecidedAction(string brainName)
    {
        return storedActions[brainName];
    }

    /// <summary>
    /// Returns the memories corresponding to the provided Brain that were
    /// received from the Python API during the last call to
    /// <see cref="ReceiveActions"></see>
    /// </summary>
    /// <returns>The memories for all agents attached to the brain,
    /// indexed by the agent ids.</returns>
    /// <param name="brainName">Brain name.</param>
    public Dictionary<int, float[]> GetMemories(string brainName)
    {
        return storedMemories[brainName];
    }

    /// <summary>
    /// Returns the values corresponding to the provided Brain that were
    /// received from the Python API during the last call to
    /// <see cref="ReceiveActions"></see>
    /// </summary>
    /// <returns>The values for all agents attached to the brain,
    /// indexed by the agent ids.</returns>
    /// <param name="brainName">Brain name.</param>
    public Dictionary<int, float> GetValues(string brainName)
    {
        return storedValues[brainName];
    }
}