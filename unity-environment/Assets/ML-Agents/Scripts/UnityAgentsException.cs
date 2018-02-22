using UnityEngine;

/// <summary>
/// Contains exceptions specific to ML-Agents.
/// </summary>
[System.Serializable]
public class UnityAgentsException : System.Exception
{
    /// <summary>
    /// When a UnityAgentsException is called, the timeScale is set to 0.
    /// The simulation will end since no steps will be taken.
    /// </summary>
    public UnityAgentsException(string message) : base(message)
    {
        Time.timeScale = 0f;
    }

    /// <summary>
    /// A constructor is needed for serialization when an exception propagates 
    /// from a remoting server to the client.
    /// </summary>
    protected UnityAgentsException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
    { }
}
