﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math;
using MLAgents;
using System.IO;
using KerasSharp.Backends;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

public struct TakeActionOutput
{
    public float[] outputAction;
    public float[] allProbabilities; //used for RL
    public float value;//use for RL
    //public Dictionary<Agent, float[]> memory;
    //public Dictionary<Agent, string> textAction;
}

/// <summary>
/// Interface for trainer
/// </summary>
/// 
[DefaultExecutionOrder(-1000)]
public abstract class TrainerBase:MonoBehaviour
{

    protected Academy academyRef;
    public InternalLearningBrain BrainToTrain;

    protected virtual void Awake()
    {
        academyRef = FindObjectOfType<Academy>();
        Debug.Assert(academyRef != null, "No Academy in this scene!");
        BrainToTrain.trainerBase = this;
    }
    
    /// <summary>
    /// impelment all of your initialization here
    /// </summary>
    public abstract void Initialize();

    /// <summary>
    /// Return the max steps of the training.
    /// </summary>
    /// <returns>max steps</returns>
    public abstract int GetMaxStep();

    /// <summary>
    /// return current steps.
    /// </summary>
    /// <returns>curren steps</returns>
    public abstract int GetStep();

    /// <summary>
    /// This will be called every fixed update when training is enabled.
    /// </summary>
    public abstract void IncrementStep();

    /// <summary>
    /// Reset your trainer
    /// </summary>
    public abstract void ResetTrainer();

    /// <summary>
    /// This will be called when an action on a agent is requested. Implement your logic to return the actions to take based on agent's current states.
    /// </summary>
    /// <param name="agentInfos">the information of agents that need actions.</param>
    /// <returns>a disionary of agent and its action to take</returns>
    public abstract Dictionary<Agent, TakeActionOutput> TakeAction(Dictionary<Agent, AgentInfoInternal> agentInfos);

    /// <summary>
    /// This will be called every loop when when training is enabled. You should record the infos of the agents based on the need of your algorithm.
    /// </summary>
    /// <param name="currentInfo">infomation of the agents before the action taken.</param>
    /// <param name="newInfo">infomation of the agents after tha ction taken</param>
    /// <param name="actionOutput">the action taken</param>
    public abstract void AddExperience(Dictionary<Agent, AgentInfoInternal> currentInfo, Dictionary<Agent, AgentInfoInternal> newInfo, Dictionary<Agent, TakeActionOutput> actionOutput);

    /// <summary>
    /// Same as AddExperience(), called every loop when training. You are supposed to process the collected data for episodes or something. You can do it in AddExperience as well...This method is called right after AddExperience().
    /// </summary>
    /// <param name="currentInfo">infomation of the agents before the action taken.</param>
    /// <param name="newInfo">infomation of the agents after tha ction taken</param>
    public abstract void ProcessExperience(Dictionary<Agent, AgentInfoInternal> currentInfo, Dictionary<Agent, AgentInfoInternal> newInfo);

    /// <summary>
    /// When this returns true, UpdateModel() will be called();
    /// </summary>
    /// <returns>Whether it is ready to udpate the model.</returns>
    public abstract bool IsReadyUpdate();

    /// <summary>
    /// Put all of your logic for training the model. This is called when IsReadyUpdate()  returns true.
    /// </summary>
    public abstract void UpdateModel();

    /// <summary>
    /// Return whether training is enabled. AddExperience(), ProcessExperience() and UpdateModel() will not be called if it returns false.
    /// </summary>
    /// <returns></returns>
    public abstract bool IsTraining();


    /// <summary>
    /// The final action received by Agent will be post processed by this. The experience record will not be processed by this. Just return the input if you dont need any processing
    /// </summary>
    /// <returns>processed action</returns>
    public abstract float[] PostprocessingAction(float[] rawAction);
}


/// <summary>
/// A abstract class for trainer if you want to save some time impelmenting ITrainer...It provides some helper functions and stuff..., you can use this as based class instead of TrainerBase.
/// </summary>
public abstract class Trainer : TrainerBase
{

    
    public LearningModelBase modelRef;
    public bool isTraining;
    protected bool prevIsTraining;

    public Action<bool> onIsTrainingChanged;

    [ShowAllPropertyAttr]
    public TrainerParams parameters;
    public bool continueFromCheckpoint = true;
    public string checkpointPath = @"Assets";
    public string checkpointFileName = @"testcheckpoint.bytes";
    private readonly string stepsSavingKey = "UnityTrainerSteps";

    [SerializeField]
    [Tooltip("Current steps of the trainer.")]
    protected int steps = 0;



    protected virtual void Start()
    {
        prevIsTraining = isTraining;
        academyRef.SetIsInference(!isTraining);
    }
    public virtual void Update()
    {
        if (prevIsTraining != isTraining)
        {
            onIsTrainingChanged?.Invoke(isTraining);
            prevIsTraining = isTraining;
            academyRef.SetIsInference(!isTraining);
        }
    }


    protected virtual void FixedUpdate()
    {
        if (isTraining && modelRef.Initialized)
            modelRef.SetLearningRate(parameters.learningRate);

        /*if (IsReadyUpdate() && isTraining && GetStep() <= GetMaxStep())   //moved into CoreBrainInternalTrainable
        {
            UpdateModel();
        }*/
    }
    
    public override int GetMaxStep()
    {
        return parameters.maxTotalSteps;
    }

    public override int GetStep()
    {
        return steps;
    }
    public override void IncrementStep()
    {
        steps++;
        if (steps % parameters.saveModelInterval == 0)
        {
            SaveModel();
        }
    }

    public override void ResetTrainer()
    {
        steps = 0;
    }

    public override float[] PostprocessingAction(float[] rawAction)
    {
        return rawAction;
    }


    //public abstract Dictionary<Agent, TakeActionOutput> TakeAction(Dictionary<Agent, AgentInfoInternal> agentInfos);
    //public abstract void AddExperience(Dictionary<Agent, AgentInfoInternal> currentInfo, Dictionary<Agent, AgentInfoInternal> newInfo, Dictionary<Agent, TakeActionOutput> actionOutput);
    //public abstract void ProcessExperience(Dictionary<Agent, AgentInfoInternal> currentInfo, Dictionary<Agent, AgentInfoInternal> newInfo);
    //public abstract bool IsReadyUpdate();
    //public abstract void UpdateModel();


    /// <summary>
    /// save the model to the checkpoint path.
    /// </summary>
    public void SaveModel()
    {
        if (string.IsNullOrEmpty(checkpointFileName))
        {
            Debug.Log("checkpointFileName empty. model not saved.");
            return;
        }
        var dataDic = modelRef.SaveCheckpoint();
        dataDic[stepsSavingKey] = new int[] { steps };

        //serailzie the data and save it to path
        var binFormatter = new BinaryFormatter();
        var mStream = new MemoryStream();
        binFormatter.Serialize(mStream, dataDic);
        byte[] data = mStream.ToArray();
        var fullPath = Path.GetFullPath(Path.Combine(checkpointPath, checkpointFileName));
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        fullPath = fullPath.Replace('\\', Path.DirectorySeparatorChar);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllBytes(fullPath, data);
        Debug.Log("Checkpoint saved to " + fullPath);
    }

    /// <summary>
    /// Load the model ffrom the checkpointpath
    /// </summary>
    public void LoadModel()
    {
        if (string.IsNullOrEmpty(checkpointFileName))
        {
            Debug.Log("checkpointFileName empty. model not loaded.");
            return;
        }
        string fullPath = Path.GetFullPath(Path.Combine(checkpointPath, checkpointFileName));
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        fullPath = fullPath.Replace('\\', Path.DirectorySeparatorChar);
        if (!File.Exists(fullPath))
        {
            Debug.Log("Model checkpoint not exist at: " + fullPath);
            return;
        }
        var bytes = File.ReadAllBytes(fullPath);
        var mStream = new MemoryStream(bytes);
        var binFormatter = new BinaryFormatter();
        var deserializedData = binFormatter.Deserialize(mStream);
        if (deserializedData is Dictionary<string, Array>)
        {
            var dataDic = deserializedData as Dictionary<string, Array>;
            modelRef.SetAllModelWeights(dataDic);
            modelRef.SetAllOptimizerWeights(dataDic);
            if (dataDic.ContainsKey(stepsSavingKey))
            {
                steps = (int)dataDic[stepsSavingKey].GetValue(0);
            }
            Debug.Log("Checkpoint loaded from " + fullPath);
        }
        else
        {
            Debug.LogError("Not recognized datatype to restoed from");
        }

        //load the datas for trainer

    }




    /// <summary>
    /// Create the visual input batch that can be used directly to feed neural network for all agents's camera visual inputs.
    /// </summary>
    /// <param name="currentInfo">Agents and their infomation wiht visual texture data</param>
    /// <param name="agentList">List of agents that needs to be included in the output</param>
    /// <param name="cameraResolutions">camera resolution data. Should be obtain from the Brain.</param>
    /// <returns>List of visual input batch data. Each item in the list is for item in cameraResolution parameter</returns>
    public static List<float[,,,]> CreateVisualInputBatch(Dictionary<Agent, AgentInfoInternal> currentInfo, List<Agent> agentList, MLAgents.Resolution[] cameraResolutions)
    {
        if (cameraResolutions == null || cameraResolutions.Length <= 0)
            return null;
        if (currentInfo.Count <= 0 || agentList.Count <= 0)
            return null;
        var observationMatrixList = new List<float[,,,]>();
        var dataHolder = new List<float[,,]>();

        for (int observationIndex = 0; observationIndex < cameraResolutions.Length; observationIndex++)
        {
            dataHolder.Clear();
            foreach (Agent agent in agentList)
            {
                dataHolder.Add(currentInfo[agent].visualObservations[observationIndex]);
            }
            observationMatrixList.Add(dataHolder.Stack());
        }

        return observationMatrixList;
    }

    public static List<float[,,,]> CreateVisualInputBatch(List<List<float[,,]>> episodeHistory, MLAgents.Resolution[] cameraResolutions)
    {
        if (cameraResolutions == null || cameraResolutions.Length <= 0)
            return null;
        var observationMatrixList = new List<float[,,,]>();
        var dataHolder = new List<float[,,]>();

        for (int observationIndex = 0; observationIndex < cameraResolutions.Length; observationIndex++)
        {
            dataHolder.Clear();
            foreach (var o in episodeHistory[observationIndex])
            {
                dataHolder.Add(o);
            }
            observationMatrixList.Add(dataHolder.Stack());
        }

        return observationMatrixList;
    }


    /// <summary>
    /// Create vector observation batch data  that can be used directly to feed neural network.
    /// </summary>
    /// <param name="currentInfo">Agents and their infomation with vector observation</param>
    /// <param name="agentList">List of agents that needs to be included in the output</param>
    /// <returns>bacth vector observation data.</returns>
    public static float[,] CreateVectorInputBatch(Dictionary<Agent, AgentInfoInternal> currentInfo, List<Agent> agentList)
    {
        if (currentInfo.Count <= 0 || agentList.Count <= 0)
            return null;
        int obsSize = currentInfo[agentList[0]].stackedVectorObservation.Count; 
        if (obsSize == 0)
            return null;
        var result = new float[agentList.Count, obsSize];

        int i = 0;
        foreach (Agent agent in agentList)
        {
            result.SetRow(i, currentInfo[agent].stackedVectorObservation.ToArray());
            i++;
        }

        return result;
    }




    public static List<float[,]> CreateActionMasks(Dictionary<Agent, AgentInfoInternal> currentInfo, List<Agent> agentList, int[] actionSizes)
    {

        if (currentInfo.Count <= 0 || agentList.Count <= 0)
            return null;
        List<float[,]> masks = new List<float[,]>();
        int agentCount = agentList.Count;
        int currentBranchStartIndex = 0;
        for (int b = 0; b < actionSizes.Length; b++)
        {
            float[,] mask = new float[agentList.Count, actionSizes[b]];
            int actionSize = actionSizes[b];
            for (int i = 0; i < agentCount; ++i)
            {
                var agentMasks = currentInfo[agentList[i]].actionMasks;
                if (agentMasks == null)
                {
                    for (int j = 0; j < actionSize; ++j)
                    {
                        mask[i, j] = 1;
                    }
                }
                else
                {
                    for (int j = 0; j < actionSize; ++j)
                    {
                        mask[i, j] = agentMasks[currentBranchStartIndex + j] ? 0 : 1;
                    }
                }
            }
            currentBranchStartIndex += actionSize;
            masks.Add(mask);
        }

        return masks;
    }


    public static List<float[,]> CreateActionMasks(List<List<float>> actionMasksHistory, int[] actionSizes)
    {
        if (actionMasksHistory == null || actionMasksHistory.Count <= 0 || actionMasksHistory[0].Count <= 0)
            return null;
        List<float[,]> masks = new List<float[,]>();
        int dataCount = actionMasksHistory[0].Count / actionSizes[0];

        for (int b = 0; b < actionSizes.Length; b++)
        {
            float[,] mask = new float[dataCount, actionSizes[b]];

            int typeSize = Marshal.SizeOf(typeof(float));
            Buffer.BlockCopy(actionMasksHistory[b].ToArray(), 0, mask, 0, actionMasksHistory[b].Count * typeSize);
            masks.Add(mask);
        }

        return masks;
    }


    public override bool IsTraining()
    {
        return isTraining;
    }
}
