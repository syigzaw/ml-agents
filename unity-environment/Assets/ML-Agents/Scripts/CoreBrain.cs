using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface which defines the functions needed for a CoreBrain.
/// </summary>
public interface CoreBrain
{
    /// <summary>
    /// Implement setBrain so let the coreBrain know what brain is using it
    /// </summary>
    void SetBrain(Brain b);

    /// <summary>
    /// Implement this method to initialize CoreBrain
    /// </summary>
    void InitializeCoreBrain();

    /// <summary>
    /// Implement this method to define the logic for deciding actions
    /// </summary>
    void DecideAction();

    /// <summary>
    /// Implement this method to define the logic for sending the actions
    /// </summary>
    void SendState();

    /// <summary>
    /// Implement this method to define what should be displayed in the brain
    /// Editor.
    /// </summary>
    void OnInspector();
}
