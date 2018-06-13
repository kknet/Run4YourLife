﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Run4YourLife.Player;

namespace Run4YourLife.Player.CustomEditors
{
    [CustomEditor(typeof(SkillBase),true)]  
    [CanEditMultipleObjects]
    public class SkillBaseEditor : Editor
    {
        protected SerializedProperty phase;
        SerializedProperty m_skillTriggerClip;
        SerializedProperty m_cooldown;
        private void OnEnable()
        {
            Init();
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(phase);
            EditorGUILayout.PropertyField(m_skillTriggerClip);
            EditorGUILayout.PropertyField(m_cooldown);

            OnGuiPhase1();

            SkillBase.Phase actualPhase = (SkillBase.Phase)phase.intValue;
            if (actualPhase != SkillBase.Phase.PHASE1)
            {
                OnGuiPhase2();
            }
            if (actualPhase == SkillBase.Phase.PHASE3)
            {
                OnGuiPhase3();
            }


            serializedObject.ApplyModifiedProperties();
        }

        public void Init()
        {
            phase = serializedObject.FindProperty("phase");
            m_skillTriggerClip = serializedObject.FindProperty("m_skillTriggerClip");
            m_cooldown = serializedObject.FindProperty("m_cooldown");
        }

        virtual public void OnGuiPhase1() {}
        virtual public void OnGuiPhase2() {}
        virtual public void OnGuiPhase3() {}
    }
}