﻿using System.Collections;
using System.Collections.Generic;
using MLAgents;
using UnityEngine;

public class PongAgentDecision : AgentDependentDecision {
    public override float[] Decide(List<float> vectorObs, List<float[,,]> visualObs, List<float> heuristicAction, List<float> otherInfomation = null)
    {
        if (agent.brain.brainParameters.vectorActionSpaceType == SpaceType.discrete)
        {
            float[] result = new float[1];
            result[0] = vectorObs[0] > vectorObs[3] ? 0 : 2;
            return result;
        }
        else
        {
            float[] result = new float[1];
            result[0] = vectorObs[0] > vectorObs[3] ? -1 : 1;
            return result;
        }
    }
}
