﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using System.Linq;

namespace MLAgents
{
/*
 This code is meant to modify the behavior of the inspector on Brain Components.
 Depending on the type of brain that is used, the available fields will be modified in the inspector accordingly.
*/
    [CustomEditor(typeof(NewBrain))]
    public class NewBrainEditor : Editor
    {
        
        [SerializeField] bool _Foldout = true;
        
        [SerializeField] private bool broadcast = true;
        public override void OnInspectorGUI()
        {
            
            
            NewBrain myBrain = (NewBrain) target;
            SerializedObject serializedBrain = serializedObject;

            BrainParameters parameters = myBrain.brainParameters;
            if (parameters.vectorActionDescriptions == null ||
                parameters.vectorActionDescriptions.Length != parameters.vectorActionSize)
                parameters.vectorActionDescriptions = new string[parameters.vectorActionSize];

            serializedBrain.Update();


            _Foldout = EditorGUILayout.Foldout(_Foldout, "Brain Parameters");
            int indentLevel = EditorGUI.indentLevel;
            if (_Foldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Vector Observation");
                EditorGUI.indentLevel++;

                SerializedProperty bpVectorObsType =
                    serializedBrain.FindProperty("brainParameters.vectorObservationSpaceType");
                EditorGUILayout.PropertyField(bpVectorObsType, new GUIContent("Space Type",
                    "Corresponds to whether state " +
                    "vector contains a single integer (Discrete) " +
                    "or a series of real-valued floats (Continuous)."));

                SerializedProperty bpVectorObsSize =
                    serializedBrain.FindProperty("brainParameters.vectorObservationSize");
                EditorGUILayout.PropertyField(bpVectorObsSize, new GUIContent("Space Size",
                    "Length of state " +
                    "vector for brain (In Continuous state space)." +
                    "Or number of possible values (in Discrete state space)."));


                SerializedProperty bpNumStackedVectorObs =
                    serializedBrain.FindProperty("brainParameters.numStackedVectorObservations");
                EditorGUILayout.PropertyField(bpNumStackedVectorObs, new GUIContent(
                    "Stacked Vectors", "Number of states that" +
                                       " will be stacked before beeing fed to the neural network."));

                EditorGUI.indentLevel--;
                SerializedProperty bpCamResol =
                    serializedBrain.FindProperty("brainParameters.cameraResolutions");
                EditorGUILayout.PropertyField(bpCamResol, new GUIContent("Visual Observation",
                    "Describes height, " +
                    "width, and whether to greyscale visual observations for the Brain."), true);

                EditorGUILayout.LabelField("Vector Action");
                EditorGUI.indentLevel++;

                SerializedProperty bpVectorActionType =
                    serializedBrain.FindProperty("brainParameters.vectorActionSpaceType");
                EditorGUILayout.PropertyField(bpVectorActionType, new GUIContent("Space Type",
                    "Corresponds to whether state" +
                    " vector contains a single integer (Discrete) " +
                    "or a series of real-valued floats (Continuous)."));

                SerializedProperty bpVectorActionSize =
                    serializedBrain.FindProperty("brainParameters.vectorActionSize");
                EditorGUILayout.PropertyField(bpVectorActionSize, new GUIContent("Space Size",
                    "Length of action vector " +
                    "for brain (In Continuous state space)." +
                    "Or number of possible values (In Discrete action space)."));

                SerializedProperty bpVectorActionDescription =
                    serializedBrain.FindProperty("brainParameters.vectorActionDescriptions");
                EditorGUILayout.PropertyField(bpVectorActionDescription, new GUIContent(
                    "Action Descriptions", "A list of strings used to name" +
                                           " the available actions for the Brain."), true);

            }

            EditorGUI.indentLevel = indentLevel;


            serializedBrain.ApplyModifiedProperties();

            
            
            
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            broadcast = EditorGUILayout.Toggle(new GUIContent("Broadcast",
                "If checked, the brain will broadcast states and actions to Python."), broadcast);
        }
    }
}