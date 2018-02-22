using System.Collections.Generic;
using System.Linq;

using UnityEngine;

/// <summary>
/// Lists the different types of Brains depending on whether TensorFlow
/// is enabled.
/// </summary>
#if ENABLE_TENSORFLOW
public enum BrainType
{
    Player,
    Heuristic,
    External,
    Internal
}
#else
public enum BrainType
{
    Player,
    Heuristic,
    External,
}
#endif

/// <summary>
/// Lists the different types of vector observations and action variables.
/// </summary>
public enum StateType
{
    discrete,
    continuous
}
;

/// <summary>
/// Structure that enables the specification of image dimensions and color
/// channels from within the Editor.
/// </summary>
[System.Serializable]
public struct Resolution
{
    /// <summary>
    // The width of the observation in pixels.
    /// </summary>
    public int width;

    /// <summary>
    // The height of the observation in pixels.
    /// </summary>
    public int height;

    /// <summary>
    // Flag that determines whether the generated image should be black/white
    // (i.e. one channel) or RGB color (3 channels).
    /// </summary>
    public bool blackAndWhite;
}

/// <summary>
/// Class that represents the Brain parameters provided in the Editor.
/// </summary>
[System.Serializable]
public class BrainParameters
{
    /// <summary>
    /// Specifies whether the state space is discrete or continuous. Discrete
    /// implies that the Agents observations will always be one of a
    /// pre-defined number of observations (e.g. position on a game board),
    /// while continuous implies that the Agents observations are infinite,
    /// and smooth (e.g. degree of rotation on a robot arm).
    /// </summary>
    [Tooltip("Specifies whether the state space is discrete (fixed, finite " +
             "cardinality) or continuous (infinite possibilities).")]
    public StateType stateSpaceType = StateType.continuous;

    /// <summary>
    /// Dimensionality of the state vector. For discrete state spaces this
    /// would be the number of unique states but for continuous state spaces,
    /// this would be the number of state observations.
    /// </summary>
    [Tooltip("Dimensionality of the state vector. For discrete state " +
             "spaces, enter the number of unique states, but for " +
             "continuous state spaces enter the number of observations.")]
    public int stateSize = 1;

    /// <summary>
    /// State observations across multiple (successive) environment steps
    /// are stacked to represent the history of the Agent. This parameter
    /// controls how many environment steps to use. Limited between 1 and 10.
    /// </summary>
    [Tooltip("The number of historical state observations to stack.")]
    [Range(1, 10)]
    public int stackedStates = 1;

    /// <summary>
    /// Specifies whether the action space is discrete or continuous. Discrete
    /// implies that the Agents actions will always be one of a pre-defined 
    /// number of actions (e.g. move left, right, up or down), while continuous
    /// implies that the Agents actions are infinite, and smooth (e.g. rotate
    /// the arm 4.5 degrees north).
    /// </summary>
    [Tooltip("Specifies whether the action space is discrete (fixed, finite " +
             "cardinality) or continuous (infinite possibilities).")]
    public StateType actionSpaceType = StateType.discrete;

    /// <summary>
    /// Dimensionality of the action vector. For continuous action spaces, this
    /// would be the number of elements in the action vector, but for
    /// discrete action spaces this would be the number of unique actions.
    /// </summary>
    [Tooltip("Dimensionality of the action vector. For continuous action " +
             "spaces, enter the number of elements in the action vector, but " +
             "for discrete action spaces enter the number of unique actions.")]
    public int actionSize = 1;

    /// <summary>
    /// A list of human-readable strings describing what each action correponds
    /// to. The dimensionality must match that <see cref="actionSize"/>.
    /// </summary>
    [Tooltip("A list human-readable strings describing what each action " +
             "correponds to.")]
    public string[] actionDescriptions;

    /// <summary>
    /// Length of the memory vector used by the Trainer. The memory acts as
    /// a cache that the Trainer uses for each Agent.
    /// </summary>
    [Tooltip("Length of memory vector for the Brain. Used with recurrent " +
             "networks.")]
    public int memorySize = 0;

    /// <summary>
    /// List of camera settings for the Brain.
    /// </summary>
    [Tooltip("List of camera settings.")]
    public Resolution[] cameraResolutions;
}

/// <summary>
/// Brain is the entitiy within ML-Agents that makes decisions for the Agent
/// GameObjects that it is linked to.
/// </summary>
[HelpURL("https://github.com/Unity-Technologies/ml-agents/blob/" +
         "master/docs/Agents-Editor-Interface.md#brain")]
public class Brain : MonoBehaviour
{
    /// Default number of agents - used to initialize data structure capacity.
    const int DEFAULT_NUM_AGENTS = 32;

    /// <summary>
    /// The current state observations for all the Agents. Key is Agent unique
    /// identifier.
    /// </summary>
    public Dictionary<int, List<float>> currentStates =
        new Dictionary<int, List<float>>(DEFAULT_NUM_AGENTS);

    /// <summary>
    /// The current list of cameras for all the Agents. Key is Agent unique
    /// identifier.
    /// </summary>
    public Dictionary<int, List<Camera>> currentCameras =
        new Dictionary<int, List<Camera>>(DEFAULT_NUM_AGENTS);

    /// <summary>
    /// The current reward for all the Agents. Key is Agent unique identifier.
    /// </summary>
    public Dictionary<int, float> currentRewards =
        new Dictionary<int, float>(DEFAULT_NUM_AGENTS);

    /// <summary>
    /// The current done flag for all the Agents. Key is Agent unique
    /// identifier.
    /// </summary>
    public Dictionary<int, bool> currentDones =
        new Dictionary<int, bool>(DEFAULT_NUM_AGENTS);

    /// <summary>
    /// The current max-step-reached flag for all the Agents. Key is Agent
    /// unique identifier.
    /// </summary>
    public Dictionary<int, bool> currentMaxes =
        new Dictionary<int, bool>(DEFAULT_NUM_AGENTS);

    /// <summary>
    /// The current actions for all the Agents. Key is Agent unique identifier.
    /// </summary>
    public Dictionary<int, float[]> currentActions =
        new Dictionary<int, float[]>(DEFAULT_NUM_AGENTS);

    /// <summary>
    /// The current memory for all the Agents. Key is Agent unique identifier.
    /// </summary>
    public Dictionary<int, float[]> currentMemories =
        new Dictionary<int, float[]>(DEFAULT_NUM_AGENTS);

    /// <summary>
    /// Defines Brain specific parameters that will be shared by all Agent
    /// objects linked to it.
    /// </summary>
    [Tooltip("Define state, observation, and action spaces for the Brain.")]
    public BrainParameters brainParameters = new BrainParameters();

    /// <summary>
    /// Defines what is the type of the Brain: External, Internal, Player
    /// or Heuristic.
    /// </summary>
    [Tooltip("Describes the type of Brain, which dictates how decisions " +
             "will be made.")]
    public BrainType brainType;

    /// <summary>
    /// Keeps track of the Agent objects which subscribe to this Brain.
    /// </summary>
    [HideInInspector]
    public Dictionary<int, Agent> agents = new Dictionary<int, Agent>();

    /// <summary>
    /// The core Brain settings. This allows the settings provided within the
    /// Editor to persist across different Brain type selections. The length
    /// of this array will be the size of the active BrainType
    /// </summary>
    [SerializeField]
    ScriptableObject[] coreBrains;

    /// <summary>
    /// Reference to the current CoreBrain used by the Brain.
    /// </summary>
    public CoreBrain coreBrain;

    /// Ensures the coreBrains are not duplicated with the brains.
    [SerializeField]
    int instanceID;

    /// <summary>
    /// Refreshes all the core brains. Method is called whenever there are
    /// any changes to the Brain type selection in the Editor.
    /// </summary>
    public void UpdateCoreBrains()
    {
        var activeBrainTypes = System.Enum.GetValues(typeof(BrainType));

        // Undefined core brains implies that the Brain object was just
        // instantiated.
        if (coreBrains == null)
        {
            int numBrainTypes = System.Enum.GetValues(typeof(BrainType)).Length;
            coreBrains = new ScriptableObject[numBrainTypes];
            foreach (BrainType activeBrainType in activeBrainTypes)
            {
                string brainClassName =
                    "CoreBrain" + activeBrainType.ToString();
                int brainId = (int)activeBrainType;
                coreBrains[brainId] =
                    ScriptableObject.CreateInstance(brainClassName);
            }

        }
        else
        {
            foreach (BrainType activeBrainType in activeBrainTypes)
            {
                int brainId = (int)activeBrainType;

                // If we cannot fit all the core brains, this means that
                // the number of brain types has changes (increased) and we
                // need to resize CoreBrains. This is taken care of later.
                if (brainId >= coreBrains.Length)
                {
                    break;
                }
                if (coreBrains[brainId] == null)
                {
                    string brainClassName =
                        "CoreBrain" + activeBrainType.ToString();
                    coreBrains[brainId] =
                        ScriptableObject.CreateInstance(brainClassName);
                }
            }
        }

        // If the length of CoreBrains does not match the number of BrainTypes, 
        // we increase the length of CoreBrains.
        if (coreBrains.Length < activeBrainTypes.Length)
        {
            ScriptableObject[] newCoreBrains =
                new ScriptableObject[activeBrainTypes.Length];
            foreach (BrainType activeBrainType in activeBrainTypes)
            {
                int brainId = (int)activeBrainType;
                if (brainId < coreBrains.Length)
                {
                    newCoreBrains[brainId] = coreBrains[brainId];
                }
                else
                {
                    string brainClassName =
                        "CoreBrain" + activeBrainType.ToString();
                    newCoreBrains[brainId] =
                        ScriptableObject.CreateInstance(brainClassName);
                }
            }
            coreBrains = newCoreBrains;
        }

        // If the stored instanceID does not match the current instanceID, 
        // this means that the Brain GameObject was duplicated, and
        // we need to make a new copy of each CoreBrain.
        if (instanceID != gameObject.GetInstanceID())
        {
            foreach (BrainType activeBrainType in activeBrainTypes)
            {
                int brainId = (int)activeBrainType;
                if (coreBrains[brainId] == null)
                {
                    string brainClassName =
                        "CoreBrain" + activeBrainType.ToString();
                    coreBrains[brainId] =
                        ScriptableObject.CreateInstance(brainClassName);
                }
                else
                {
                    coreBrains[brainId] =
                        ScriptableObject.Instantiate(coreBrains[brainId]);
                }
            }
            instanceID = gameObject.GetInstanceID();
        }

        // The coreBrain to display is the one defined in brainType
        coreBrain = (CoreBrain)coreBrains[(int)brainType];

        coreBrain.SetBrain(this);
    }

    /// <summary>
    /// Initializes the Brain at the very beginning of the environment.
    /// </summary>
    public void InitializeBrain()
    {
        UpdateCoreBrains();
        coreBrain.InitializeCoreBrain();
    }

    /// <summary>
    /// Collects the information (e.g. states, camera, rewards) for all the
    /// Agent object linked to this Brain.
    /// </summary>
    public void CollectEverything()
    {
        currentStates.Clear();
        currentCameras.Clear();
        currentRewards.Clear();
        currentDones.Clear();
        currentMaxes.Clear();
        currentActions.Clear();
        currentMemories.Clear();

        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            idAgent.Value.SetCumulativeReward();

            List<float> states = CollectAgentState(idAgent.Value);
            List<Camera> observations = CollectAgentCamera(idAgent.Value);

            // Load up all the Agent information into the corresponding
            // data structures.
            currentStates.Add(idAgent.Key, states);
            currentCameras.Add(idAgent.Key, observations);
            currentRewards.Add(idAgent.Key, idAgent.Value.reward);
            currentDones.Add(idAgent.Key, idAgent.Value.done);
            currentMaxes.Add(idAgent.Key, idAgent.Value.maxStepReached);
            currentActions.Add(idAgent.Key, idAgent.Value.agentStoredAction);
            currentMemories.Add(idAgent.Key, idAgent.Value.memory);
        }
    }

    /// <summary>
    /// Collects the states of all the agents which subscribe to this brain 
    /// and returns a dictionary {id -> state}
    /// </summary>
    public Dictionary<int, List<float>> CollectStates()
    {
        currentStates.Clear();
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            idAgent.Value.SetCumulativeReward();

            List<float> states = CollectAgentState(idAgent.Value);
            currentStates.Add(idAgent.Key, states);
        }
        return currentStates;
    }

    /// <summary>
    /// Collects the observations of all the agents which subscribe to this 
    /// brain and returns a dictionary {id -> Camera}
    /// </summary>
    public Dictionary<int, List<Camera>> CollectObservations()
    {
        currentCameras.Clear();
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            List<Camera> observations = CollectAgentCamera(idAgent.Value);
            currentCameras.Add(idAgent.Key, observations);
        }
        return currentCameras;

    }

    /// <summary>
    /// Collects the rewards of all the agents which subscribe to this brain
    /// and returns a dictionary {id -> reward}
    /// </summary>
    public Dictionary<int, float> CollectRewards()
    {
        currentRewards.Clear();
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            currentRewards.Add(idAgent.Key, idAgent.Value.reward);
        }
        return currentRewards;
    }

    /// <summary>
    /// Collects the done flag of all the agents which subscribe to this brain
    /// and returns a dictionary {id -> done}
    /// </summary>
    public Dictionary<int, bool> CollectDones()
    {
        currentDones.Clear();
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            currentDones.Add(idAgent.Key, idAgent.Value.done);
        }
        return currentDones;
    }

    /// <summary>
    /// Collects the done flag of all the agents which subscribe to this brain
    /// and returns a dictionary {id -> done}
    /// </summary>
    public Dictionary<int, bool> CollectMaxes()
    {
        currentMaxes.Clear();
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            currentMaxes.Add(idAgent.Key, idAgent.Value.maxStepReached);
        }
        return currentMaxes;
    }

    /// <summary>
    /// Collects the actions of all the agents which subscribe to this brain 
    /// and returns a dictionary {id -> action}
    /// </summary>
    public Dictionary<int, float[]> CollectActions()
    {
        currentActions.Clear();
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            currentActions.Add(idAgent.Key, idAgent.Value.agentStoredAction);
        }
        return currentActions;
    }

    /// <summary>
    /// Collects the memories of all the agents which subscribe to this brain 
    /// and returns a dictionary {id -> memories}
    /// </summary>
    public Dictionary<int, float[]> CollectMemories()
    {
        currentMemories.Clear();
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            currentMemories.Add(idAgent.Key, idAgent.Value.memory);
        }
        return currentMemories;
    }

    /// <summary>
    /// Takes a dictionary {id -> memories} and sends the memories to the 
    /// corresponding agents
    /// </summary>
    public void SendMemories(Dictionary<int, float[]> memories)
    {
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            idAgent.Value.memory = memories[idAgent.Key];
        }
    }

    /// <summary>
    /// Takes a dictionary {id -> actions} and sends the actions to the 
    /// corresponding agents
    /// </summary>
    public void SendActions(Dictionary<int, float[]> actions)
    {
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            //Add a check here to see if the component was destroyed ?
            idAgent.Value.UpdateAction(actions[idAgent.Key]);
        }
    }

    /// <summary>
    /// Takes a dictionary {id -> values} and sends the values to the 
    /// corresponding agents
    /// </summary>
    public void SendValues(Dictionary<int, float> values)
    {
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            //Add a check here to see if the component was destroyed ?
            idAgent.Value.value = values[idAgent.Key];
        }
    }

    /// <summary>
    /// Sets all the agents which subscribe to the brain to done
    /// </summary>
    public void SendDone()
    {
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            idAgent.Value.done = true;
        }
    }

    /// <summary>
    /// Sets all the agents which subscribe to the brain to maxStepReached
    /// </summary>
    public void SendMaxReached()
    {
        foreach (KeyValuePair<int, Agent> idAgent in agents)
        {
            idAgent.Value.maxStepReached = true;
        }
    }

    /// <summary>
    /// Uses coreBrain to call SendState on the CoreBrain
    /// </summary>
    public void SendState()
    {
        coreBrain.SendState();
    }

    /// <summary>
    /// Uses coreBrain to call decideAction on the CoreBrain
    /// </summary>
    public void DecideAction()
    {
        coreBrain.DecideAction();
    }

    /// <summary>
    /// Is used by the Academy to send a step message to all the agents 
    /// which are not done
    /// </summary>
    public void Step()
    {
        var agentsToIterate = agents.Values.ToList();
        foreach (Agent agent in agentsToIterate)
        {
            if (!agent.done)
            {
                agent.Step();
            }
        }
    }

    /// <summary>
    /// Is used by the Academy to reset the agents if they are done
    /// </summary>
    public void ResetIfDone()
    {
        var agentsToIterate = agents.Values.ToList();
        foreach (Agent agent in agentsToIterate)
        {
            if (agent.done)
            {
                if (!agent.resetOnDone)
                {
                    agent.AgentOnDone();
                }
                else
                {
                    agent.Reset();
                }
            }
        }
    }

    /// <summary>
    /// Is used by the Academy to reset all agents 
    /// </summary>
    public void Reset()
    {
        foreach (Agent agent in agents.Values)
        {
            agent.Reset();
            agent.done = false;
            agent.maxStepReached = false;
        }
    }

    /// <summary>
    /// Is used by the Academy reset the done flag and the rewards of the
    /// agents that subscribe to the brain
    /// </summary>
    public void ResetDoneAndReward()
    {
        foreach (Agent agent in agents.Values)
        {
            if (!agent.done || agent.resetOnDone)
            {
                agent.ResetReward();
                agent.done = false;
                agent.maxStepReached = false;
            }
        }
    }

    /// <summary>
    /// Contains logic for coverting a camera component into a Texture2D.
    /// </summary>
    public Texture2D ObservationToTex(Camera camera, int width, int height)
    {
        Camera cam = camera;
        Rect oldRec = camera.rect;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        bool supportsAntialiasing = false;
        bool needsRescale = false;
        var depth = 24;
        var format = RenderTextureFormat.Default;
        var readWrite = RenderTextureReadWrite.Default;
        var antiAliasing = (supportsAntialiasing) ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;

        var finalRT = RenderTexture.GetTemporary(
            width, height, depth, format, readWrite, antiAliasing);
        var renderRT = (!needsRescale) ? finalRT : RenderTexture.GetTemporary(
            width, height, depth, format, readWrite, antiAliasing);
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        var prevActiveRT = RenderTexture.active;
        var prevCameraRT = cam.targetTexture;

        // render to offscreen texture (readonly from CPU side)
        RenderTexture.active = renderRT;
        cam.targetTexture = renderRT;

        cam.Render();

        if (needsRescale)
        {
            RenderTexture.active = finalRT;
            Graphics.Blit(renderRT, finalRT);
            RenderTexture.ReleaseTemporary(renderRT);
        }

        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();
        cam.targetTexture = prevCameraRT;
        cam.rect = oldRec;
        RenderTexture.active = prevActiveRT;
        RenderTexture.ReleaseTemporary(finalRT);
        return tex;
    }

    /// <summary>
    /// Contains logic to convert the agent's cameras into observation list
    /// (as list of float arrays).
    public List<float[,,,]> GetObservationMatrixList(List<int> agent_keys)
    {
        var observation_matrix_list = new List<float[,,,]>();
        Dictionary<int, List<Camera>> observations = CollectObservations();
        int num_obs = brainParameters.cameraResolutions.Length;
        for (int obs_number = 0; obs_number < num_obs; obs_number++)
        {
            var width = brainParameters.cameraResolutions[obs_number].width;
            var height = brainParameters.cameraResolutions[obs_number].height;
            var bw = brainParameters.cameraResolutions[obs_number].blackAndWhite;
            var pixels = 0;
            if (bw)
                pixels = 1;
            else
                pixels = 3;
            float[,,,] observation_matrix = new float[agent_keys.Count
            , height
            , width
            , pixels];
            var i = 0;
            foreach (int k in agent_keys)
            {
                Camera agent_obs = observations[k][obs_number];
                Texture2D tex = ObservationToTex(agent_obs, width, height);
                for (int w = 0; w < width; w++)
                {
                    for (int h = 0; h < height; h++)
                    {
                        Color c = tex.GetPixel(w, h);
                        if (!bw)
                        {
                            observation_matrix[i, tex.height - h - 1, w, 0] = c.r;
                            observation_matrix[i, tex.height - h - 1, w, 1] = c.g;
                            observation_matrix[i, tex.height - h - 1, w, 2] = c.b;
                        }
                        else
                        {
                            observation_matrix[i, tex.height - h - 1, w, 0] =
                                (c.r + c.g + c.b) / 3;
                        }
                    }
                }
                UnityEngine.Object.DestroyImmediate(tex);
                Resources.UnloadUnusedAssets();
                i++;
            }
            observation_matrix_list.Add(observation_matrix);
        }
        return observation_matrix_list;
    }

    /// Collects the state information for an Agent and verifies its
    /// dimensionality.
    List<float> CollectAgentState(Agent agent)
    {
        List<float> states = agent.ClearAndCollectState();

        // Check the dimensionality of the state observation.
        int expectedStateDimensionality = brainParameters.stackedStates;
        if (brainParameters.stateSpaceType == StateType.continuous)
        {
            expectedStateDimensionality *= brainParameters.stateSize;
        }
        if (states.Count != expectedStateDimensionality)
        {
            throw new UnityAgentsException(
                string.Format(
                    @"The number of states does not match for
                        agent {0}: Was expecting {1} states but received {2}.",
                    agent.gameObject.name,
                    brainParameters.stateSize,
                    states.Count));
        }
        return states;
    }

    /// Collects the camera information for an Agent and verifies its
    /// dimensionality.
    List<Camera> CollectAgentCamera(Agent agent)
    {
        List<Camera> observations = agent.observations;

        // Check the dimensionality of the camera obserations.
        if (observations.Count < brainParameters.cameraResolutions.Count())
        {
            throw new UnityAgentsException(
                string.Format(
                    @"The number of observations does not match for
                        agent {0}: Was expecting at least {1} observation but 
                        received {2}.",
                    agent.gameObject.name,
                    brainParameters.cameraResolutions.Count(),
                    observations.Count));
        }
        return observations;
    }
}
