using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Agent Monobehavior class that is attached to a Unity GameObject, making it
/// an Agent. An agent produces observations and takes actions in the 
/// environment. Observations are determined by the cameras attached 
/// to the agent in addition to the vector observations implemented by the
/// user in <see cref="CollectState"/>. On the other hand, actions
/// are determined by decisions produced by a linked Brain. Currently, this
/// class is expected to be extended to implement the desired agent behavior.
/// </summary>
/// <remarks>
/// Simply speaking, an agent roams through an environment and at each step
/// of the environment extracts its current observation, sends them to its
/// linked brain and in return receives an action from its brain. In practice,
/// however, an agent need not send its observation at every step since very
/// little may have changed between sucessive steps. Currently, how often an
/// agent updates its brain with a fresh observation is determined by the
/// Academy. 
/// 
/// At any step, an agent may be considered <see cref="done"/>. 
/// This could occur due to a variety of reasons:
///     - The agent reached an end state within its environment.
///     - The agent reached the maximum # of steps (i.e. timed out).
///     - The academy reached the maximum # of steps (forced agent to be done).
/// 
/// Here, an agent reaches an end state if it completes its task successfully
/// or somehow fails along the way. In the case where an agent is done before
/// the academy, it either resets and restarts, or just lingers until the
/// academy is done.
/// 
/// An important note regarding steps and episodes is due. Here, an agent step
/// corresponds to an academy step, which also corresponds to Unity
/// environment step (i.e. each FixedUpdate call). This is not the case for
/// episodes. The academy controls the global episode count and each agent 
/// controls its own local episode count and can reset and start a new local
/// episode independently (based on its own experience). Thus an academy
/// (global) episode can be viewed as the upper-bound on an agents episode
/// length and that within a single global episode, an agent may have completed
/// multiple local episodes. Consequently, if an agent <see cref="maxStep"/> is
/// set to a value larger than the academy max steps value, then the academy
/// value takes precedence (since the agent max step will never be reached).
/// 
/// Lastly, note that at any step the brain linked to the agent is allowed to
/// change programmatically with <see cref="GiveBrain"/>.
/// 
/// Implementation-wise, it is recommended that this class is extended and the
/// virtual methods overridden. For sample implementations of agent behavior,
/// see the Examples/ directory within this Unity project.
/// </remarks>
[HelpURL("https://github.com/Unity-Technologies/ml-agents/blob/" +
         "master/docs/Agents-Editor-Interface.md#agent")]
public abstract class Agent : MonoBehaviour
{
    [Tooltip("The Brain to register this Agent to. Can be dragged into " +
             "the inspector using the Editor.")]
    /// <summary>
    /// The Brain attached to this agent. A brain can
    /// be attached either directly from the Editor or programmatically
    /// through <see cref="GiveBrain"/>. It is OK for an agent to not have
    /// a brain, as long as no decision is requested.
    /// </summary>
    public Brain brain;

    [Tooltip("A list of Cameras which will be used to generate observations.")]
    /// <summary>
    /// The list of the Camera GameObjects the agent uses for visual
    /// observations.
    /// </summary>
    public List<Camera> observations;

    [Tooltip("The maximum number of steps allowed for the Agent.")]
    /// <summary>
    /// The maximum number of steps the agent takes before being done. 
    /// </summary>
    /// <remarks>
    /// If set to 0, the agent can only be set to done programmatically (or
    /// when the Academy is done).
    /// If set to any positive integer, the agent will be set to done after
    /// that many steps. Note that setting the max step to a value greater
    /// than the academy max step value renders it useless.
    /// </remarks>
    public int maxStep;

    [Tooltip("Specifies whether the Agent should reset when it is done " +
             "or wait for the Academy to complete the episode.")]
    /// <summary>
    /// Determines the behaviour of the agent when done.
    /// </summary>
    /// <remarks>
    /// If true, the agent will reset when done and start a new episode.
    /// Otherwise, the agent will remain done and its behavior will be
    /// dictated by the <see cref="AgentOnDone"/> method.
    /// </remarks>
    public bool resetOnDone = true;

    /// Most recent agent vector (i.e. numeric) observation.
    [HideInInspector]
    public List<float> state;

    /// The previous agent vector observations, stacked. The length of the
    /// history (i.e. number of vector observations to stack) is specified
    /// in the Brain parameters.
    [HideInInspector]
    public List<float> stackedStates;

    /// Represents the reward the agent accumulated during the current step.
    /// It is reset to 0 at the beginning of every step.
    /// Should be set to a positive value when the agent performs a "good"
    /// action that we wish to reinforce/reward, and set to a negative value
    /// when the agent performs a "bad" action that we wish to punish/deter.
    [HideInInspector]
    public float reward;

    /// Whether or not the agent has completed the episode. This may be due
    /// to either reaching a success or fail state, or reaching the maximum
    /// number of steps (i.e. timing out).
    [HideInInspector]
    public bool done;

    /// Whether or not the agent reached the maximum number of steps.
    [HideInInspector]
    public bool maxStepReached;

    /// The current value estimate of the agent. An external Brain can pass
    /// the value estimate to the agent at every step. If Monitor is
    /// attached to the agent, this value will be visualized in the Editor
    /// during inference mode.
    [HideInInspector]
    public float value;

    /// Keeps track of the cumulative reward in this episode.
    [HideInInspector]
    public float cumulativeReward;

    /// Keeps track of the number of steps taken by the agent in this episode.
    /// Note that this value is different for each agent, and may not overlap
    /// with the step counter in the Academy, since agents reset based on
    /// their own experience.
    [HideInInspector]
    public int stepCounter;

    /// Keeps track of the last action taken by the Brain.
    [HideInInspector]
    public float[] agentStoredAction;

    /// Used by the Trainer to store information about the agent. This data
    /// structure is not consumed or modified by the agent directly, they are
    /// just the owners of their trainiers memory. Currently, however, the
    /// size of the memory is in the Editor properties for the Brain.
    [HideInInspector]
    public float[] memory;

    /// Unique identifier each agent receives at initialization. It is used
    /// to separate between different agents in the environment.
    [HideInInspector]
    public int id;

    /// Monobehavior function that is called when the attached GameObject
    /// becomes enabled or active.
    void OnEnable()
    {
        id = gameObject.GetInstanceID();
        AttachAgentToBrain(gameObject.GetComponent<Agent>(), brain);
        ResetData();
        InitializeAgent();
    }

    /// Helper function that resets all the data structures associated with
    /// the agent. Typically used when the agent is being initialized or reset
    /// at the end of an episode.
    void ResetData()
    {
        if (brain != null)
        {
            // retrieve data structure sizes
            int actionSize = 1;
            if (brain.brainParameters.actionSpaceType == StateType.continuous)
            {
                actionSize = brain.brainParameters.actionSize;
            }
            int stateSize = brain.brainParameters.stateSize;
            int stackDepth = brain.brainParameters.stackedStates;
            int memorySize = brain.brainParameters.memorySize;

            // initialize and reset data structures
            agentStoredAction = new float[actionSize];
            memory = new float[memorySize];
            if (state == null || stackedStates == null)
            {
                state = new List<float>(stateSize);
                stackedStates = new List<float>(stateSize * stackDepth);
            }
            else
            {
                state.Clear();
                stackedStates.Clear();
            }
            stackedStates.AddRange(new float[stateSize * stackDepth]);
        }
    }

    /// Monobehavior function that is called when the attached GameObject
    /// becomes disabled or inactive.
    void OnDisable()
    {
        DetachAgentFromBrain(id, brain);
    }

    /// <summary>
    /// Updates the Brain for the agent. Any brain currently assigned to the
    /// agent will be replaced with the provided one.
    /// </summary>
    /// <remarks>
    /// The agent unsubscribes from its current brain (if it has one) and
    /// subscribes to the provided brain. This enables contextual brains, that
    /// is, updating the behaviour (hence brain) of the agent depending on
    /// the context of the game. For example, we may utilize one (wandering)
    /// brain when an agent is randomly exploring an open world, but switch
    /// to another (fighting) brain when it comes into contact with an enemy.
    /// </remarks>
    /// <param name="newBrain">New brain to subscribe this agent to</param>
    public void GiveBrain(Brain newBrain)
    {
        SwapBrain(newBrain);
        ResetData();
    }

    /// <summary>
    /// Helper method that detaches an agent from a Brain.
    /// </summary>
    /// <param name="agentId">The agents unique identifier</param>
    /// <param name="brain">The brain that the agent should detach from</param>
    static void DetachAgentFromBrain(int agentId, Brain brain)
    {
        if (brain != null)
        {
            brain.agents.Remove(agentId);
        }
    }

    /// <summary>
    /// Helper method that attaches an agent to a Brain.
    /// </summary>
    /// <param name="agent">The agent reference</param>
    /// <param name="brain">The brain that the agent should attach to</param>
    static void AttachAgentToBrain(Agent agent, Brain brain)
    {
        if (brain != null)
        {
            brain.agents.Add(agent.id, agent);
        }
    }

    /// <summary>
    /// Swaps the current Brain attached to the agent with the provided one.
    /// Swapping involves first detatching any brain currently linked to the
    /// agent and then attaching the agent to the new, provided brain.
    /// </summary>
    /// <param name="newBrain">New brain to attach to the agent.</param>
    void SwapBrain(Brain newBrain)
    {
        DetachAgentFromBrain(id, brain);
        brain = newBrain;
        AttachAgentToBrain(gameObject.GetComponent<Agent>(), newBrain);
    }

    /// <summary>
    /// Initializes the agent, called once when the agent is enabled. Can be
    /// left empty if there is no special, unique set-up behavior for the
    /// agent.
    /// </summary>
    /// <remarks>
    /// One sample use is to store local references to other objects in the
    /// scene which would facilitate computing this agents observation.
    /// </remarks>
    public virtual void InitializeAgent()
    {
    }

    /// <summary>
    /// Updates the stacked observation object with a fresh current
    /// observation.
    /// </summary>
    /// <returns>
    /// Stacked observation with the latest current observation
    /// </returns>
    public List<float> ClearAndCollectState()
    {
        state.Clear();
        CollectState();
        stackedStates.RemoveRange(0, brain.brainParameters.stateSize);
        stackedStates.AddRange(state);
        return stackedStates;
    }

    /// <summary>
    /// Collects the vector observation (here, called state) of the agent.
    /// The agent observation numerically describes the current environment
    /// from the perspective of the agent. When overriding this method, ensure
    /// that the <see cref="state"/> variable is both modified and returned.
    /// </summary>
    /// <remarks>
    /// Simply, an agents observation is any environment
    /// information that helps the Agent acheive its goal. For example, for 
    /// a fighting Agent, its observation could include distances to friends
    /// or enemies, or the current level of ammunition at its disposal.
    /// </remarks>
    /// <returns>
    /// Agent observation. The length of the returned list must match the state
    /// size specified in the attached Brain. For  discrete state spaces,
    /// the length of the array should be 1.
    /// </returns>
    public virtual List<float> CollectState()
    {
        return state;
    }

    /// <summary>
    /// Specifies the agent behavior at every step based on the provided
    /// action.
    /// </summary>
    /// <param name="action">
    /// Action to take. Note that for discrete actions, the provided array
    /// will be of length 1.
    /// </param>
    public virtual void AgentStep(float[] action)
    {

    }

    /// <summary>
    /// Specifies the agent behavior when done and <see cref="resetOnDone"/>
    /// is false. This method can be used to remove the agent from the scene.
    /// </summary>
    public virtual void AgentOnDone()
    {

    }

    /// <summary>
    /// Specifies the agent behavior when being reset, which can be due to
    /// the agent or Academy being done (i.e. completion of local or global
    /// episode).
    /// </summary>
    public virtual void AgentReset()
    {

    }

    /// <summary>
    /// An internal reset method that updates internal data structures in
    /// addition to calling <see cref="AgentReset"/>.
    /// </summary>
    public void Reset()
    {
        ResetData();
        stepCounter = 0;
        AgentReset();
        cumulativeReward = -reward;
    }

    /// <summary>
    /// Returns the current agent reward, <see cref="reward"/>.
    /// </summary>
    /// <returns>The reward.</returns>
    public float CollectReward()
    {
        return reward;
    }

    /// <summary>
    /// Updates the cumulative reward, <see cref="cumulativeReward"/>.
    /// </summary>
    public void SetCumulativeReward()
    {
        if (!done)
        {
            cumulativeReward += reward;
        }
        else
        {
            cumulativeReward = 0f;
        }
    }

    /// <summary>
    /// Returns the agents done flag, <see cref="done"/>.
    /// </summary>
    /// <returns>whether the agent is done or not.</returns>
    public bool CollectDone()
    {
        return done;
    }

    /// <summary>
    /// Updates the agent action. Must be called before <see cref="Step"/>
    /// to ensure that the provided action is used when stepping.
    /// </summary>
    /// <param name="action">New agent action.</param>
    public void UpdateAction(float[] action)
    {
        agentStoredAction = action;
    }

    /// <summary>
    /// An internal step method that is called by the Brain to indicate that
    /// an environment step has occured. In addition to updating internal
    /// variables, the <see cref="AgentStep"/> method is called with the
    /// latest action.
    /// </summary>
    public void Step()
    {
        AgentStep(agentStoredAction);
        stepCounter += 1;
        if ((stepCounter > maxStep) && (maxStep > 0))
        {
            done = true;
            maxStepReached = true;
        }
    }

    /// <summary>
    /// Resets the agents reward. This is necessary before each new
    /// agent step.
    /// </summary>
    public void ResetReward()
    {
        reward = 0;
    }
}
