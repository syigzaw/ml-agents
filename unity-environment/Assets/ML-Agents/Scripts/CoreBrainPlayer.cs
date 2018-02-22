using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Core Brain that is based on Player inputs. Supports the
/// Broadcast feature where Player action, states and rewards are sent to
/// the Python API to facilitate learning policies based on real Player
/// behaviors.
/// </summary>
public class CoreBrainPlayer : ScriptableObject, CoreBrain
{
    /// <summary>
    /// Flag indicating whether the broadcast feature should be enabled or not.
    /// </summary>
    [SerializeField]
    [Tooltip("If checked, the Brain will broadcast observations and actions to Python.")]
    bool broadcast = true;

    [System.Serializable]
    struct DiscretePlayerAction
    {
        public KeyCode key;
        public int value;
    }

    [System.Serializable]
    struct ContinuousPlayerAction
    {
        public KeyCode key;
        public int index;
        public float value;
    }

    /// <summary>
    /// External communicator used for sending and receiving messages.
    /// </summary>
    ExternalCommunicator coord;

    /// Contains the mapping from input to continuous actions
    [SerializeField]
    [Tooltip("The list of keys and the value they correspond to for continuous control.")]
    ContinuousPlayerAction[] continuousPlayerActions;

    /// Contains the mapping from input to discrete actions
    [SerializeField]
    [Tooltip("The list of keys and the value they correspond to for discrete control.")]
    DiscretePlayerAction[] discretePlayerActions;

    [SerializeField]
    int defaultAction = 0;

    /// <summary>
    /// Reference to the brain that uses this CoreBrainExternal.
    /// </summary>
    public Brain brain;

    /// <summary> <inheritdoc/> </summary>
    public void SetBrain(Brain b)
    {
        brain = b;
    }

    /// <summary> <inheritdoc/> </summary>
    public void InitializeCoreBrain()
    {
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
        if (brain.brainParameters.actionSpaceType == StateType.continuous)
        {
            var action = new float[brain.brainParameters.actionSize];
            foreach (ContinuousPlayerAction cha in continuousPlayerActions)
            {
                if (Input.GetKey(cha.key))
                {
                    action[cha.index] = cha.value;
                }
            }
            var actions = new Dictionary<int, float[]>();
            foreach (KeyValuePair<int, Agent> idAgent in brain.agents)
            {
                actions.Add(idAgent.Key, action);
            }
            brain.SendActions(actions);
        }
        else
        {
            var action = new float[1] { defaultAction };
            foreach (DiscretePlayerAction dha in discretePlayerActions)
            {
                if (Input.GetKey(dha.key))
                {
                    action[0] = (float)dha.value;
                    break;
                }
            }
            var actions = new Dictionary<int, float[]>();
            foreach (KeyValuePair<int, Agent> idAgent in brain.agents)
            {
                actions.Add(idAgent.Key, action);
            }
            brain.SendActions(actions);
        }
    }

    /// <summary> <inheritdoc/> </summary>
    public void SendState()
    {
        if (coord != null)
        {
            coord.SendBrainInfo(brain);
        }
        else
        {
            // States are collected in order to debug the CollectStates method.
            brain.CollectStates();
        }
    }

    /// <summary> <inheritdoc/> </summary>
    public void OnInspector()
    {
#if UNITY_EDITOR
        RefreshGUI();
#endif
    }

    void RefreshGUI()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        broadcast = EditorGUILayout.Toggle(
            new GUIContent(
                "Broadcast",
                "If checked, the brain will broadcast states and actions."),
            broadcast);
        var serializedBrain = new SerializedObject(this);
        if (brain.brainParameters.actionSpaceType == StateType.continuous)
        {
            GUILayout.Label("Edit the continuous inputs for you actions",
                            EditorStyles.boldLabel);
            var chas = serializedBrain.FindProperty("continuousPlayerActions");
            serializedBrain.Update();
            EditorGUILayout.PropertyField(chas, true);
            serializedBrain.ApplyModifiedProperties();
            if (continuousPlayerActions == null)
            {
                continuousPlayerActions = new ContinuousPlayerAction[0];
            }
            foreach (ContinuousPlayerAction cha in continuousPlayerActions)
            {
                if (cha.index >= brain.brainParameters.actionSize)
                {
                    EditorGUILayout.HelpBox(
                        string.Format(
                            @"Key {0} is assigned to index {1} but the 
                                action size is only of size {2}",
                            cha.key.ToString(),
                            cha.index.ToString(),
                            brain.brainParameters.actionSize.ToString()),
                        MessageType.Error);
                }
            }

        }
        else
        {
            GUILayout.Label("Edit the discrete inputs for you actions",
                            EditorStyles.boldLabel);
            defaultAction = EditorGUILayout.IntField("Default Action",
                                                     defaultAction);
            var dhas = serializedBrain.FindProperty("discretePlayerActions");
            serializedBrain.Update();
            EditorGUILayout.PropertyField(dhas, true);
            serializedBrain.ApplyModifiedProperties();
        }
    }
}
