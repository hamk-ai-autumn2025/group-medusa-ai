using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float deltaTime { get; private set; }
    public float dynamicTimeScale = 1f;
    public float gravity { get; private set; }
    public float dynamicGravityScale = 1f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        deltaTime = Time.deltaTime * dynamicTimeScale;
        gravity = Physics2D.gravity.y * dynamicGravityScale;
    }
}
