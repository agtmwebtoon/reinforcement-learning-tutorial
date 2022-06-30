using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// A hummingbird Machine Learning Agent
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")] 
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agent's camera")] public Camera agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode;
    
    //The rigidbody of the agent
    new private Rigidbody rigidbody;
    
    //The flower area that the agent is in
    private FlowerArea flowerArea;

    private Flower nearestFlower;

    private float smoothPitchChange = 0f;
    private float smoothYawChange = 0f;

    private const float MaxPitchAngle = 80f;
    private const float BeakTipRadius = 0.008f;
    
    //Whether the agent is frozen (intentionally not flying)
    private bool frozen = false;
    
    /// <summary>
    /// The amount of nectar the agent has obtained this episode
    /// </summary>
    public float NectarObtained { get; private set; }

    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();
        
        // If not training mode, no max step, play forever
        if (!trainingMode) MaxStep = 0;
        

    }
    
    /// <summary>
    /// Reset the agent when an episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            //Only reset flowers in training when there is one agent per area
            flowerArea.ResetFlowers();
        }
        
        // Reset nectar obtained
        NectarObtained = 0f;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        
        // Default to spawning in front of a flower
        bool inFrontOfFlower = true;
        if (trainingMode)
        {
            inFrontOfFlower = UnityEngine.Random.value > .5f;
        }

        MoveToSafeRandomPosition(inFrontOfFlower);
        UpdateNearestFlower();

    }
    
    /// <summary>
    /// Called when and action is received from either the player input or neural network
    ///
    /// vectorAction[i] represents:
    /// Index 0: move vector x (+1 = right, -1 = left) 
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4: yaw angle (+1 = turn right, -1 = turn left)
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        // Don't take action if frozen
        if (frozen) return;
        
        // Calculate movement vector
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        
        //Add force in the direction of the move vector
        rigidbody.AddForce(move * moveForce);
        
        // Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];
        
        
        // Calculate smooth rotation changes
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;

        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
        
        // Apply the new rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
    
    /// <summary>
    /// Collect vector observation from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {

        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }
        //Observe the agent's local rotation (4 observations)
        sensor.AddObservation(transform.localRotation.normalized);
        
        // Get a vector from the beak tip to the nearest flower (3 observations)
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;
        sensor.AddObservation(toFlower.normalized);

        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));
        
        // Observe a dot product that indicates whether the beak is pointing toward the flower
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
        
        //Observe the relative distance from the beak tip to the flower
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);
    }

    /// <summary>
    /// When behavior Type is set to "Heyristic Only" on the agent's Behavior Parameters, <br />
    /// this function will be called. Its return values will be fed into
    /// <see cref="OnActionReceived"/> Instead of using the neural network
    /// </summary>
    /// <param name="actionsOut">And output action array</param>
    public override void Heuristic(float[] actionsOut)
    {
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;


        if (Input.GetKey((KeyCode.W))) forward = transform.forward;
        else if (Input.GetKey((KeyCode.S))) forward = -transform.forward;
        
        if (Input.GetKey((KeyCode.A))) left = -transform.right;
        else if (Input.GetKey((KeyCode.D))) left = transform.right;
        
        if (Input.GetKey((KeyCode.E))) up = transform.up;
        else if (Input.GetKey((KeyCode.C))) up = -transform.up;

        if (Input.GetKey((KeyCode.UpArrow))) pitch = 1f;
        else if (Input.GetKey((KeyCode.DownArrow))) pitch = -1f;
        
        if (Input.GetKey((KeyCode.LeftArrow))) yaw = -1f;
        else if (Input.GetKey((KeyCode.RightArrow))) yaw = 1f;

        Vector3 combined = (forward + left + up).normalized;

        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;
    }

    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }
    
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }


    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                float distanceFromFlower = UnityEngine.Random.Range(.1f, .2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                float height = UnityEngine.Random.Range(1.2f, 2.5f);
                float radius = UnityEngine.Random.Range(2f, 7f);
                Quaternion direction = quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);
                potentialPosition = flowerArea.transform.position + Vector3.up * height +
                                    direction * Vector3.forward * radius;

                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }
    
    /// <summary>
    /// Update the nearest flower to the agent
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNector)
            {
                // No current nearest flower and this flower has nectar, so set to this flower
                nearestFlower = flower;
            }
            
            else if (flower.HasNector)
            {
                // Calculate distance to this flower and distance to the current nearest flower
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower =
                    Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                if (!nearestFlower.HasNector || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }
    
    /// <summary>
    /// Called when agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }
    
    /// <summary>
    /// Called when agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }
    
    private void TriggerEnterOrStay(Collider collider)
    {
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                float nectarReceived = flower.Feed(.01f);
                NectarObtained += nectarReceived;

                if (trainingMode)
                {
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized,
                        -nearestFlower.FlowerUpVector.normalized));
                    AddReward(.01f + bonus);
                }

                if (!flower.HasNector)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            AddReward(-.5f);
        }
    }

    private void Update()
    {
        // Draw a line from the beak tip to the nearest flower
        if (nearestFlower != null)
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
    }

    private void FixedUpdate()
    {
        if (nearestFlower != null && !nearestFlower.HasNector)
            UpdateNearestFlower();
    }
}
