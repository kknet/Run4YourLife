﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FXReceiver : MonoBehaviour {

    public Animator anim;

    public GameObject PlayFx(GameObject prefab)
    {
        return FXManager.Instance.InstantiateFromReceiver(this, prefab);
    }

    public void PlayFxOnAnimation(string animName,float time, GameObject prefab)
    {
        AnimationPlayOnTimeManager.Instance.PlayOnAnimation(anim,animName,time,()=>CallBack(prefab));
    }

    void CallBack(GameObject prefab)
    {
        FXManager.Instance.InstantiateFromReceiver(this,prefab);
    }
}
