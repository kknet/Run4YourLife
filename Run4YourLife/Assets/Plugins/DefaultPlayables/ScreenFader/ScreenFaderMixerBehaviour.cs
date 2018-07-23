using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class ScreenFaderMixerBehaviour : PlayableBehaviour
{
    Color m_DefaultColor;
    bool leaveColor = false;

    Image m_TrackBinding;
    bool m_FirstFrameHappened;
    bool m_FirstFrameInsideForHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_TrackBinding = playerData as Image;

        if (m_TrackBinding == null)
            return;

        if (!m_FirstFrameHappened)
        {
            m_DefaultColor = m_TrackBinding.color;
            m_FirstFrameHappened = true;
        }

        int inputCount = playable.GetInputCount ();

        Color blendedColor = Color.clear;
        float totalWeight = 0f;
        float greatestWeight = 0f;
        int currentInputs = 0;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<ScreenFaderBehaviour> inputPlayable = (ScriptPlayable<ScreenFaderBehaviour>)playable.GetInput(i);
            ScreenFaderBehaviour input = inputPlayable.GetBehaviour ();

            if ( i==0 & input.leaveColor)
            {
                leaveColor = true;
            }

            float t = (float)(inputPlayable.GetTime() * input.inverseDuration);
            blendedColor += Color.Lerp(m_DefaultColor,input.color,t ) * inputWeight;
            totalWeight += inputWeight;

            if (inputWeight > greatestWeight)
            {
                greatestWeight = inputWeight;
            }

            if (!Mathf.Approximately (inputWeight, 0f))
                currentInputs++;
        }
        m_FirstFrameInsideForHappened = true;

        m_TrackBinding.color = blendedColor + m_DefaultColor * (1f - totalWeight);
    }

    public override void OnPlayableDestroy (Playable playable)
    {
        m_FirstFrameHappened = false;
        m_FirstFrameInsideForHappened = false;

        if (m_TrackBinding == null)
            return;

        if (!leaveColor)
        {
            m_TrackBinding.color = m_DefaultColor;
        }
    }
}
