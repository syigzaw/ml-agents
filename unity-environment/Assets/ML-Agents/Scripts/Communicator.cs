using System.Collections.Generic;

/// <summary>
/// AcademyParameters is a struct containing basic information about the 
/// learning environment. This contains the information needed by the 
/// Python API at start-up, which is sent through the Communicator.
/// </summary>
public struct AcademyParameters
{
    /// <summary>
    /// The name of the Academy GameObject as defined by the user in the
    /// Editor.
    /// </summary>
    public string AcademyName;

    /// <summary>
    /// The API version for the communicator. This defines the API contract
    /// between the learning environment and Python API. Any changes to
    /// the API warrant a new, updated verion.
    /// </summary>
    public string apiNumber;

    /// <summary>
    /// The location of the logfile. This logfile is created and written
    /// to by the learning environment, but shared between the
    /// learning environment and the Python API.
    /// </summary>
    public string logPath;

    /// <summary>
    /// The default reset parameters as specified by the user in the Unity
    /// Editor.
    /// </summary>
    public Dictionary<string, float> resetParameters;

    /// <summary>
    /// A list of the names of all the Brain GameObjects specified as children
    /// of the Academy GameObject within the Editor.
    /// </summary>
    public List<string> brainNames;

    /// <summary>
    /// A list of the BrainParameter values of all the Brain GameObjects
    /// specified as children of the Academy GameObject within the Unity
    /// Editor. Each entry in this list corresponds to the respective entry
    /// in <see cref="brainNames"/>, that is the 3rd entry in both lists
    /// correspond to the same brain.
    /// </summary>
    public List<BrainParameters> brainParameters;

    /// <summary>
    /// A list of the names of all the External Brain GameObjects specified as
    /// children of the Academy GameObject within the Editor. This list
    /// will contain a subset of the names in <see cref="brainNames"/>.
    /// </summary>
    public List<string> externalBrainNames;
}

/// <summary>
/// Lists the commands that can be sent from a communicator.
/// </summary>
public enum ExternalCommand
{
    STEP,
    RESET,
    QUIT
}

/// <summary>
/// A Communicator is a layer that handles communication between the 
/// Unity learning environment (specifically, the Academy) and the Python API. 
/// </summary>
public interface Communicator
{
    /// <summary>
    /// Register a Brain with the Python API. One call is needed for each 
    /// Brain.
    /// </summary>
    /// <param name="brain">Brain to register.</param>
    void SubscribeBrain(Brain brain);

    /// <summary>
    /// First contact between the learning environment and communicator. Tests
    /// that the communicator is alive and functioning.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if handshake was successful; <c>false</c> otherwise.
    /// </returns>
    bool CommunicatorHandShake();

    /// <summary>
    /// Initialize the communicator. Must be called before any of the
    /// communication-methods are called, <see cref="ReceiveActions"/>,
    /// <see cref="ReceiveCommand"/>, and <see cref="ReceiveResetParameters"/>.
    /// </summary>
    void InitializeCommunicator();

    /// <summary>
    /// Receives actions from the Python API for all the brains that have
    /// been subscribed.
    /// </summary>
    void ReceiveActions();

    /// <summary>
    /// Receives the next command from the Python API.
    /// </summary>
    /// <returns>The command.</returns>
    ExternalCommand ReceiveCommand();

    /// <summary>
    /// Receives the new dictionary of reset parameters from the Python API. 
    /// </summary>
    /// <returns>The next reset parameters.</returns>
    Dictionary<string, float> ReceiveResetParameters();
}
