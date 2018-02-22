using System.Collections.Generic;

using UnityEngine;

/*! \mainpage ML-Agents Index Page
 * Welcome to Unity Machine Learning Agents documentation.
 * 
 * Glossary:
 *  - Editor: Any pane (e.g. Hierarchy, Scene) of the Unity Editor.
 *  - Trainer: Abstract term representing the entity within the Python API
 *      that trains the Agents behavior.
 */

/// <summary>
/// Wraps the environment-level parameters that are provided within the
/// Editor. These parameters can be provided for training and inference
/// modes separately and represent screen resolution, rendering quality and
/// frame rate.
/// </summary>
[System.Serializable]
public class EnvironmentConfiguration
{
    [Tooltip("Width of the environment window in pixels.")]
    public int width;

    [Tooltip("Height of the environment window in pixels.")]
    public int height;

    [Tooltip("Rendering quality of environment. (Higher is better quality.)")]
    [Range(0, 5)]
    public int qualityLevel;

    [Tooltip("Speed at which environment is run. (Higher is faster.)")]
    [Range(1f, 100f)]
    public float timeScale;

    [Tooltip("Frames per second (FPS) engine attempts to maintain.")]
    public int targetFrameRate;

    /// Initializes a new instance of the 
    /// <see cref="EnvironmentConfiguration"/> class.
    /// <param name="width">Width of environment window (pixels).</param>
    /// <param name="height">Height of environment window (pixels).</param>
    /// <param name="qualityLevel">
    /// Rendering quality of environment. Ranges from 0 to 5, with higher.
    /// </param>
    /// <param name="timeScale">
    /// Speed at which environment is run. Ranges from 1 to 100, with higher
    /// values representing faster speed.
    /// </param>
    /// <param name="targetFrameRate">
    /// Target frame rate (per second) that the engine tries to maintain.
    /// </param>
    public EnvironmentConfiguration(
        int width, int height, int qualityLevel,
        float timeScale, int targetFrameRate)
    {
        this.width = width;
        this.height = height;
        this.qualityLevel = qualityLevel;
        this.timeScale = timeScale;
        this.targetFrameRate = targetFrameRate;
    }
}


/// <summary>
/// An Academy is where Agent objects go to train their behaviors. More 
/// specifically, an academy is a collection of Brain objects and each agent
/// in a scene is attached to one brain (a single brain may be attached to 
/// multiple agents). Currently, this class is expected to be extended to
/// implement the desired academy behavior.
/// </summary>
/// <remarks>
/// When an academy is run, it can either be in inference or training mode.
/// The mode is determined by the presence or absence of a Communicator. In
/// the presence of a communicator, the academy is run in training mode where
/// the states and observations of each agent are sent through the
/// communicator. In the absence of a communciator, the academy is run in
/// inference mode where the agent behavior is determined by the brain
/// attached to it (which may be internal, heuristic or player).
/// </remarks>
[HelpURL("https://github.com/Unity-Technologies/ml-agents/blob/master/" +
         "docs/Agents-Editor-Interface.md#academy")]
public abstract class Academy : MonoBehaviour
{
    /// Struct used to enable specifiying the reset parameters (as key-value 
    /// pairs) within the Editor.
    [System.Serializable]
    struct ResetParameter
    {
        public string key;
        public float value;
    }

    [SerializeField]
    [Tooltip("Total number of steps per episode.\n" +
             "0 corresponds to episodes without a maximum number of steps.\n" +
             "Once the step counter reaches maximum, the environment" +
             " will reset.")]
    int maxSteps;

    [SerializeField]
    [Tooltip("How many steps of the environment to skip before asking" +
             " Brains for decisions.")]
    int stepsToSkip;

    [SerializeField]
    [Tooltip("How many seconds to wait between steps when running in" +
             " Inference.")]
    float waitTime;

    [SerializeField]
    [Tooltip("The engine-level settings which correspond to rendering" +
             " quality and engine speed during Training.")]
    EnvironmentConfiguration trainingConfiguration =
        new EnvironmentConfiguration(80, 80, 1, 100.0f, -1);

    [SerializeField]
    [Tooltip("The engine-level settings which correspond to rendering" +
             " quality and engine speed during Inference.")]
    EnvironmentConfiguration inferenceConfiguration =
        new EnvironmentConfiguration(1280, 720, 5, 1.0f, 60);

    [SerializeField]
    [Tooltip("List of custom parameters that can be changed in the" +
             " environment on reset.")]
    ResetParameter[] defaultResetParameters;

    /// <summary>
    /// Contains a mapping from parameter names to float values. They are
    /// used in <see cref="AcademyReset"/> and <see cref="AcademyStep"/>
    /// to modify elements in the environment at reset time.
    /// <summary/>
    /// <remarks>
    /// Default reset parameters are specified in the academy Editor via
    /// <see cref="defaultResetParameters"/>. 
    /// They can be modified when training with an external Brain by passing
    /// a config dictionary at reset. 
    /// </remarks>
    public Dictionary<string, float> resetParameters;

    /// If true, the Academy will use inference settings. This field is 
    /// initialized in <see cref="Awake"/> depending on the presence
    /// or absence of a communicator. Furthermore, it can be modified by an
    /// external Brain during reset via <see cref="SetInference"/>.
    bool isInference = true;

    /// List of Brains represented in the academy. These will be the brains
    /// that are specified as children of the academy in the Editor.
    List<Brain> brains = new List<Brain>();

    /// Stores the last command received from the Communicator.
    ExternalCommand externalCommand;

    /// Number of environment steps that have elapsed since the brains last
    /// made a decision.
    int stepsSinceDecision;

    /// Boolean flag indicating whether the current step of the environment
    /// will be skipped or the not. A skipped step means that the brains will
    /// not make a decision.
    bool skippingStep = true;

    /// Boolean flag indicating whether a communicator is accessible by the
    /// environment. This also specifies whether the environment is in
    /// Training or Inference mode.
    bool isCommunicatorOn;

    /// The time at the beginning of an environment step.
    float timeAtStep;

    /// The done flag of the academy. When set to true, the academy will
    /// call <see cref="AcademyReset"/> instead of <see cref="AcademyStep"/>
    /// at step time. If true, all agents done flags will be set to true.
    bool done;

    /// Whether the academy has reached the maximum number of steps for the
    /// current episode.
    bool maxStepReached;

    /// The number of episodes completed by the environment. Incremented 
    /// each time the environment is reset.
    int episodeCount;

    /// The number of steps completed within the current episide. Incremented
    /// each time a step is taken in the environment. Is reset to 0 during 
    /// <see cref="AcademyReset"/>.
    int currentStep;

    /// <summary>
    /// Flag that indicates whether the inference/training mode of the
    /// environment was switched by the external Brain. This impacts the
    /// engine settings at the next environment step.
    /// </summary>
    bool modeSwitched;

    /// Pointer to the communicator currently in use by the Academy.
    Communicator communicator;

    /// <summary>
    /// Monobehavior function called at the very beginning of environment
    /// creation. Academy uses this time to initialize internal data
    /// structures, initialize the environment and check for the existence
    /// of a communicator.
    /// </summary>
    void Awake()
    {
        // Load in default parameters (provided in Editor).
        LoadResetParameters(defaultResetParameters, this.resetParameters);

        // Initialize Academy and Brains.
        InitializeAcademy();
        LoadBrains(gameObject, brains);
        foreach (Brain brain in brains)
        {
            brain.InitializeBrain();
        }

        // Initialize communicator (if possible)
        communicator = new ExternalCommunicator(this);
        if (communicator.CommunicatorHandShake())
        {
            isCommunicatorOn = true;
            communicator.InitializeCommunicator();
            // Retrieve first command from external communicator (expected
            // to be a RESET command)
            externalCommand = communicator.ReceiveCommand();
        }

        // If a communicator is enabled/provided, then we assume we are in
        // training mode. In the absence of a communicator, we assume we are
        // in inference mode.
        isInference = !isCommunicatorOn;

        done = true;

        // Configure the environment using the configurations provided by
        // the developer in the Editor.
        ConfigureEnvironment();
    }

    /// <summary>
    /// Initializes the academy and environment. Called during the waking-up
    /// phase of the environment before any of the scene objects/agents have
    /// been initialized.
    /// </summary>
    public virtual void InitializeAcademy()
    {

    }

    /// <summary>
    /// Configures the environment settings depending on the training/inference
    /// mode and the corresponding parameters passed in the Editor.
    /// </summary>
    void ConfigureEnvironment()
    {
        if (isInference)
        {
            ConfigureEnvironmentHelper(inferenceConfiguration);
            Monitor.SetActive(true);
        }
        else
        {
            ConfigureEnvironmentHelper(trainingConfiguration);
            QualitySettings.vSyncCount = 0;
            Monitor.SetActive(false);
        }
    }

    /// <summary>
    /// Helper method for initializing the environment based on the provided
    /// configuration.
    /// </summary>
    /// <param name="config">
    /// Environment configuration (specified in the Editor).
    /// </param>
    static void ConfigureEnvironmentHelper(EnvironmentConfiguration config)
    {
        Screen.SetResolution(config.width, config.height, false);
        QualitySettings.SetQualityLevel(config.qualityLevel, true);
        Time.timeScale = config.timeScale;
        Application.targetFrameRate = config.targetFrameRate;
    }

    /// <summary>
    /// Specifies the academy behavior at every step of the environment.
    /// </summary>
    public virtual void AcademyStep()
    {

    }

    /// <summary>
    /// Specifies the academy behavior when being reset (i.e. at the completion
    /// of a global episode).
    /// </summary>
    public virtual void AcademyReset()
    {

    }

    /// <summary>
    /// Sets the <see cref="isInference"/> flag to the provided value. If
    /// the new flag differs from the current flag value, this signals that
    /// the environment configuration needs to be updated.
    /// </summary>
    /// <param name="isInference">
    /// Environment mode, if true then inference, otherwise training.
    /// </param>
    public void SetInference(bool isInference)
    {
        if (this.isInference != isInference)
        {
            this.isInference = isInference;

            // This signals to the academy that at the next environment step
            // the engine configurations need updating to the respective mode
            // (i.e. training vs inference) configuraiton.
            modeSwitched = true;
        }
    }

    /// <summary>
    /// Returns whether or not the academy is done.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if academy is done, <c>false</c> otherwise.
    /// </returns>
    public bool IsDone()
    {
        return done;
    }

    /// <summary>
    /// Returns whether or not the communicator is on.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if communicator is on, <c>false</c> otherwise.
    /// </returns>
    public bool IsCommunicatorOn()
    {
        return isCommunicatorOn;
    }

    /// <summary>
    /// Returns the Communicator currently used by the Academy.
    /// </summary>
    /// <returns>The commincator currently in use (may be null).</returns>
    public Communicator GetCommunicator()
    {
        return communicator;
    }

    /// <summary>
    /// Internal step method, that is called with each environment step
    /// when a decision is made. Unlike <see cref="AcademyStep"/> which is
    /// called with each environment step, this method is called whenever a
    /// decision needs to be made, which depends on <see cref="stepsToSkip"/>.
    /// </summary>
    internal void Step()
    {
        // Reset all agents whose flags are set to done.
        foreach (Brain brain in brains)
        {
            // Set all agents to done if academy is done.
            if (done)
            {
                brain.SendDone();
                brain.SendMaxReached();
            }
            brain.ResetIfDone();

            brain.SendState();

            brain.ResetDoneAndReward();
        }
    }

    /// <summary>
    /// Resets the academy and also calls the user-defined
    /// <see cref="AcademyReset"/> method. The academy is reset each time a
    /// global episode is completed.
    /// </summary>
    internal void Reset()
    {
        currentStep = 0;
        episodeCount++;
        done = false;
        maxStepReached = false;
        AcademyReset();

        foreach (Brain brain in brains)
        {
            brain.Reset();
            brain.ResetDoneAndReward();
        }
    }

    /// <summary>
    /// Instructs the Brains within the environment to take a decision.
    /// </summary>
    void DecideAction()
    {
        if (isCommunicatorOn)
        {
            communicator.ReceiveActions();
        }

        foreach (Brain brain in brains)
        {
            brain.DecideAction();
        }

        stepsSinceDecision = 0;
    }

    /// <summary>
    /// Monobehavior function that dictates each environment step.
    /// </summary>
    void FixedUpdate()
    {
        RunMdp();
    }

    /// <summary>
    /// Performs a single environment update to the Academy, Brain and Agent
    /// objects within the environment.
    /// </summary>
    void RunMdp()
    {
        if (modeSwitched)
        {
            ConfigureEnvironment();
            modeSwitched = false;
        }

        if ((isInference) && (timeAtStep + waitTime > Time.time))
        {
            return;
        }

        timeAtStep = Time.time;
        stepsSinceDecision += 1;

        currentStep += 1;
        if ((currentStep >= maxSteps) && maxSteps > 0)
        {
            done = true;
            maxStepReached = true;
        }

        if ((stepsSinceDecision > stepsToSkip) || done)
        {
            skippingStep = false;
            stepsSinceDecision = 0;
        }
        else
        {
            skippingStep = true;
        }

        if (!skippingStep)
        {
            if (isCommunicatorOn)
            {
                if (externalCommand == ExternalCommand.STEP)
                {
                    Step();
                    externalCommand = communicator.ReceiveCommand();
                }
                if (externalCommand == ExternalCommand.RESET)
                {
                    LoadResetParameters(communicator.ReceiveResetParameters(),
                                        resetParameters);
                    Reset();
                    externalCommand = ExternalCommand.STEP;
                    RunMdp();
                    return;
                }
                if (externalCommand == ExternalCommand.QUIT)
                {
                    Application.Quit();
                    return;
                }
            }
            else
            {
                if (done)
                {
                    Reset();
                    RunMdp();
                    return;
                }
                Step();
            }

            DecideAction();
        }


        AcademyStep();

        foreach (Brain brain in brains)
        {
            brain.Step();
        }
    }

    /// <summary>
    /// Helper method that loads the reset parameters from one format to
    /// another. This method essentially copies one dictionary into another,
    /// but its name is kept specific (for now) to indicate its purpose.
    /// </summary>
    /// <param name="src">Next reset parameters.</param>
    /// <param name="dst">Reset parameters.</param>
    static void LoadResetParameters(
        Dictionary<string, float> src, Dictionary<string, float> dst)
    {
        dst.Clear();
        foreach (KeyValuePair<string, float> kv in src)
        {
            dst[kv.Key] = kv.Value;
        }
    }

    /// <summary>
    /// Helper method that loads the reset parameters from one format to
    /// another. 
    /// </summary>
    /// <param name="src">Next reset parameters.</param>
    /// <param name="dst">Reset parameters.</param>
    static void LoadResetParameters(
        ResetParameter[] src, Dictionary<string, float> dst)
    {
        dst.Clear();
        foreach (ResetParameter kv in src)
        {
            dst[kv.key] = kv.value;
        }
    }

    /// <summary>
    /// Helper method that loads the Brain objects that are currently
    /// specified as children of the Academy within the Editor.
    /// </summary>
    /// <param name="academy">Academy.</param>
    /// <param name="brains">Placeholder object to load the brains to.</param>
    static void LoadBrains(GameObject academy, List<Brain> brains)
    {
        brains.Clear();
        var transform = academy.transform;
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var brain = child.GetComponent<Brain>();

            if (brain != null && child.gameObject.activeSelf)
            {
                brains.Add(brain);
            }
        }
    }
}
