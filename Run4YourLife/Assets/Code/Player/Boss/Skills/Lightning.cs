﻿using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Run4YourLife.Player {
    public class Lightning : SkillBase
    {

        #region Inspector

        [SerializeField]
        private float width;

        [SerializeField]
        private float delayHit;

        [SerializeField]
        private GameObject flashEffect;

        [SerializeField]
        private GameObject lighningEffect;

        #endregion

        private void OnEnable()
        {
            Vector3 tempPos = transform.position;
            tempPos.y = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, Mathf.Abs(Camera.main.transform.position.z - transform.position.z))).y;
            transform.position = tempPos;
            StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {       
            Transform flashBody = flashEffect.transform;
            Vector3 newSize = Vector3.one;
            newSize.x = newSize.z = width;
            float topScreen = Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.pixelHeight, Mathf.Abs(Camera.main.transform.position.z - flashBody.position.z))).y;
            newSize.y = (topScreen - transform.position.y)/2;
            flashBody.localScale = newSize;
            flashBody.localPosition = new Vector3(0, newSize.y);
            flashEffect.SetActive(true);
            yield return new WaitForSeconds(delayHit);
            flashEffect.SetActive(false);
            LightningHit();
        }

        private void LightningHit()
        {
            Vector3 pos = Vector3.zero;
            pos.y = Camera.main.ScreenToWorldPoint(new Vector3(0, Camera.main.pixelHeight, Mathf.Abs(Camera.main.transform.position.z - pos.z))).y;
            lighningEffect.transform.localPosition = pos;
            lighningEffect.SetActive(true);

            RaycastHit[] hits;
            hits = Physics.SphereCastAll(lighningEffect.transform.position, width, Vector3.down, pos.y - transform.position.y,Layers.Runner);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.tag == Tags.Runner)
                {
                    ExecuteEvents.Execute<ICharacterEvents>(hit.collider.gameObject, null, (x, y) => x.Kill());
                }
            }
        }
    }
}