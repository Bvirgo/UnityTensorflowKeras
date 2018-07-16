﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu()]
public class TrainerParamsMimic : ScriptableObject
{

    [Header("Learning related")]
    public int maxTotalSteps = 100000000;
    
    public int batchSize = 32;
    public int numIterationPerTrain = 1;
    public float learningRate = 0.001f;

    public int requiredDataBeforeTraining = 1000;
    public int maxBufferSize = 10000;
    
    [Header("Log related")]
    public int lossLogInterval = 1;
    public int actionDiffLogInterval = 100;
    public int saveModelInterval = 10000;
    /// Displays the parameters of the CoreBrainInternal in the Inspector 
    public void OnInspector()
    {
#if UNITY_EDITOR
        Editor.CreateEditor(this).OnInspectorGUI();
#endif
    }
}