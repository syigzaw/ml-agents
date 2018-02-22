using UnityEngine;

/// CoreBrain which decides actions via communication with an external system such as Python.
/// 
/// <summary>
/// Core Brain that is controlled based on the external communicator. It
/// is the primary mode used for training and for enabling custom training
/// and inference algorithms.
/// </summary>
public class CoreBrainExternal : ScriptableObject, CoreBrain
{
    /// <summary>
    /// Reference to the brain that uses this CoreBrainExternal.
    /// </summary>
    public Brain brain;

    /// <summary>
    /// External communicator used for sending and receiving messages.
    /// </summary>
    ExternalCommunicator coord;

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
        if (!academy.IsCommunicatorOn())
        {
            coord = null;
            throw new UnityAgentsException(
                string.Format(
                    @"The brain {0} was set to External mode but Unity was 
                        unable to read the arguments passed at launch.",
                    brain.gameObject.name));
        }
        if (academy.GetCommunicator() is ExternalCommunicator)
        {
            coord = (ExternalCommunicator)academy.GetCommunicator();
            coord.SubscribeBrain(brain);
        }
    }

    /// <summary> <inheritdoc/> </summary>
    public void DecideAction()
    {
        if (coord != null)
        {
            brain.SendActions(coord.GetDecidedAction(brain.gameObject.name));
            brain.SendMemories(coord.GetMemories(brain.gameObject.name));
            brain.SendValues(coord.GetValues(brain.gameObject.name));
        }
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
        // Nothing needed.
    }
}
