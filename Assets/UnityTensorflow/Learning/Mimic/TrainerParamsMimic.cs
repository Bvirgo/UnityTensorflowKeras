﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "ml-agent/sl/TrainerParamsMimic")]
public class TrainerParamsMimic : TrainerParams
{

    [Header("Learning related")]
    
    public int batchSize = 32;
    public int numIterationPerTrain = 1;

    public int requiredDataBeforeTraining = 1000;
    public int maxBufferSize = 10000;
    

}
