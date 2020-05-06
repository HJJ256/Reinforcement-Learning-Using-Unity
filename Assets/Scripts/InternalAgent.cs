using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class InternalAgent : Agent
{
    public float[][] q_table;
    float learning_rate = 0.6f;
    int action = -1;
    float gamma = 0.99f;//Discount
    float e = 1;//Epsilon
    float eMin = 0.1f;
    int annealingSteps = 6000;
    int lastState;

    public override void SendParameters(EnvironmentParameters env)
    {
        q_table = new float[env.state_size][];
        action = 0;
        //Getting q_table initialized
        for(int i = 0; i < env.state_size; i++)
        {
            q_table[i] = new float[env.action_size];
            for(int j = 0; j < env.action_size; j++)
            {
                q_table[i][j] = 0.0f;
            }
        }
    }

    //GetAction will now pick an action from policy to take from its current state 
    //and return it
    public override float[] GetAction()
    {
        action = q_table[lastState].ToList().IndexOf(q_table[lastState].Max()); //ArgMax
        if (Random.Range(0f, 1f) < e) { action = Random.Range(0, 3); }
        if (e > eMin) { e = e - ((1f - eMin) / (float)annealingSteps); }
        GameObject.Find("Etxt").GetComponent<Text>().text = "Epsilon: " + e.ToString("F2");
        float currentQ = q_table[lastState][action];
        GameObject.Find("Qtxt").GetComponent<Text>().text = "Current Q-Value: " + currentQ.ToString("F2");
        return new float[1] { action };
    }

    //GetValue fetches the value stored in the Q table
    //It returns average Q Value per state
    public override float[] GetValue()
    {
        float[] value_table = new float[q_table.Length];
        for(int i = 0; i < q_table.Length; i++)
        {
            value_table[i] = q_table[i].Average();
        }
        return value_table;
    }

    //Finally SendState function updates the Q table using the latest experience
    //here parameter state is the state in which experience happened
    //parameter reward is the reward received by agent from environment for current action
    //parameter done indicates whether episode has ended
    public override void SendState(List<float> state, float reward, bool done)
    {
        int nextState = Mathf.FloorToInt(state.First());
        if (action != -1)
        {
            if (done)
            {
                q_table[lastState][action] += learning_rate * (reward - q_table[lastState][action]);
            }
            else
            {
                q_table[lastState][action] += learning_rate * (reward + gamma * q_table[nextState].Max() - q_table[lastState][action]);
            }
        }
        lastState = nextState;
    }
}
