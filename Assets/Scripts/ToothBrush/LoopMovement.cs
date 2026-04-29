using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class LoopMovement : PausableAnim
{
    [SerializeField] private RectTransform rectTransform_;
    [SerializeField] private float loopMoveSpeed = 5f;
    [SerializeField] private float loopDistance = 200f;
    [SerializeField] private float loopRotSpeed = 0.7f;
    [SerializeField] private float loopRot = 5f;
    [SerializeField] private int direction = 1;

    private float EPSILON = 0.2f;
    private float brushImageStartY;
    private float brushImageStartYRot;
    private float time_ = 0;

    private void Start()
    {
        brushImageStartY = rectTransform_.anchoredPosition.y;
        brushImageStartYRot = rectTransform_.localEulerAngles.z;
    }

    private void Update()
    {
        if(IsPaused) {
            return;
        }

        time_ += Time.deltaTime;

        if (Math.Abs(loopDistance) > EPSILON)
        {

            float newY = brushImageStartY + direction * (Mathf.PingPong(time_ * loopMoveSpeed, Math.Abs(loopDistance)) - (loopDistance / 2f));
            rectTransform_.anchoredPosition = new Vector2(rectTransform_.anchoredPosition.x, newY);
        }

        if (Math.Abs(loopRot) > EPSILON)
        {
            float newYRot = brushImageStartYRot + direction * (Mathf.PingPong(time_ * loopRotSpeed, loopRot) - (loopRot / 2f));
            var originalEuler = rectTransform_.localEulerAngles;
            rectTransform_.localEulerAngles = new Vector3(originalEuler.x, originalEuler.y, newYRot);
        }
    }
}