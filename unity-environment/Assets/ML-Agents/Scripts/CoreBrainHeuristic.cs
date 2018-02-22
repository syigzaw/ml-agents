using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Core brain type that is based on hard-coded heuristic rules. This type
/// is helpful for both debugging behaviors and comparing a trained agent
/// from a hard-coded one.
/// </summary>
public class CoreBrainHeuristic : ScriptableObject, CoreBrain
{
    /// <summary>
    /// Flag indicating whether the broadcast feature should be enabled or not.
    /// </summary>
    [SerializeField]
    [Tooltip("If checked, the Brain will broadcast observations and actions to Python.")]
    bool broadcast = true;

    /// <summary>
    /// Reference to the brain that uses this CoreBrainExternal.
    /// </summary>
    public Brain brain;

    /// <summary>
    /// External communicator used for sending and receiving messages.
    /// </summary>
    ExternalCommunicator coord;

    /// <summary>
    /// Reference to the Decision component used to decide the actions.
    /// </summary>
    public Decision decision;

    /// <summary> <inheritdoc/> </summary>
    public void SetBrain(Brain b)
    {
        brain = b;
    }

    /// <summary> <inheritdoc/> </summary>
    public void InitializeCoreBrain()
    {
        decision = brain.gameObject.GetComponent<Decision>();
        Academy academy = brain.gameObject.transform.parent.gameObject
                       .GetComponent<Academy>();
        if (!academy.IsCommunicatorOn() || !broadcast)
        {
            coord = null;
        }
        else if (academy.GetCommunicator() is ExternalCommunicator)
        {
            coord = (ExternalCommunicator)academy.GetCommunicator();
            coord.SubscribeBrain(brain);
        }
    }

    /// <summary> <inheritdoc/> </summary>
    public void DecideAction()
    {
        if (decision == null)
        {
            throw new UnityAgentsException(
                "The Brain is set to Heuristic, but no decision script attached to it");
        }

        var actions = new Dictionary<int, float[]>();
        var new_memories = new Dictionary<int, float[]>();
        Dictionary<int, List<float>> states = brain.CollectStates();
        Dictionary<int, List<Camera>> observations = brain.CollectObservations();
        Dictionary<int, float> rewards = brain.CollectRewards();
        Dictionary<int, bool> dones = brain.CollectDones();
        Dictionary<int, float[]> old_memories = brain.CollectMemories();

        foreach (KeyValuePair<int, Agent> idAgent in brain.agents)
        {
            actions.Add(idAgent.Key, decision.Decide(
                states[idAgent.Key],
                observations[idAgent.Key],
                rewards[idAgent.Key],
                dones[idAgent.Key],
                old_memories[idAgent.Key]));
        }
        foreach (KeyValuePair<int, Agent> idAgent in brain.agents)
        {
            new_memories.Add(idAgent.Key, decision.MakeMemory(
                states[idAgent.Key],
                observations[idAgent.Key],
                rewards[idAgent.Key],
                dones[idAgent.Key],
                old_memories[idAgent.Key]));
        }
        brain.SendActions(actions);
        brain.SendMemories(new_memories);
    }

    /// <summary> <inheritdoc/> </summary>
    public void SendState()
    {
        if (coord != null)
        {
            coord.SendBrainInfo(brain);
        }
    }

    /// <summary> <inheritdoc/> </summary>
    public void OnInspector()
    {
#if UNITY_EDITOR
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        broadcast = EditorGUILayout.Toggle(
            new GUIContent("Broadcast",
                           "If checked, the brain will broadcast states and actions to Python."),
            broadcast);
        if (brain.gameObject.GetComponent<Decision>() == null)
        {
            EditorGUILayout.HelpBox(
                "You need to add a 'Decision' component to this gameObject",
                MessageType.Error);
        }
#endif
    }
}
