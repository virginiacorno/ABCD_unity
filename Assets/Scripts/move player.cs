using UnityEngine;
using UnityEngine.InputSystem; 
public class moveplayer : MonoBehaviour
{
#if UNITY_WEBGL
    private void LogData(System.Collections.Generic.Dictionary<string, object> data) => WebDataLogger.Instance.LogEvent(data);
    private float CurrentRunTime() => WebDataLogger.Instance.GetCurrentRunTime();
#else
    private void LogData(System.Collections.Generic.Dictionary<string, object> data) => DataLogger.Instance.LogEvent(data);
    private float CurrentRunTime() => DataLogger.Instance.GetCurrentRunTime();
#endif

    public float gridStepSize = 10.3f;
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 100f;

    public rewardManager rewardManager;
    //public CameraManager cameraManager;  //V: Classic camera mode
    public FreeNavigationCamera cameraManager;  //V: Free navigation mode

    public bool inputEnabled = true; //V: allows to detect key input, turned off at the end of trials when transition screens/resets are called

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isMoving = false;
    private bool isRotating = false;
    
    //V: variables to keep track of logging
    private bool rotationStartLogged = false;
    private bool movementStartLogged = false;
    
    void Start()
    {
        Vector3 startPos = rewardManager.GetStartPosition();
        transform.position = startPos;
        targetPosition = startPos;
        targetRotation = transform.rotation;
    }
    
    void Update()
    {
        if (isRotating) //V: first check if we are rotating/supposed to be rotating
        {
            RotateToTarget();
        }
        else if (!isMoving)
        {
            CheckInput();
            rewardManager.RewardFound(transform.position);
        }
        else if (isMoving)
        {
            MoveToTarget(); 
        }
    }

    public void SetPosition(Vector3 newPosition) //V: function to position the player on the grid as specified by parameters above
    {
        transform.position = newPosition;
        targetPosition = newPosition;
        isMoving = false;
        isRotating = false;
    }
    
    void CheckInput() //V: check keyboard input and set the rotation and movement targets accordingly
    {
        if (!inputEnabled) return; //V: early return if input is disabled

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;  // Safety check

        string keyPressed = null;
        float keyPressTime = CurrentRunTime();
        Vector3 oldPosition = transform.position;

        if (keyboard.upArrowKey.wasPressedThisFrame) //V: up key is the only one allowing to move, the other ones are just controlling rotations
        {
            Vector3 potentialTarget = transform.position + (transform.forward * gridStepSize);
            if (WithinBounds(potentialTarget))
            {
                targetPosition = potentialTarget;
                isMoving = true;
            }
            cameraManager.DisableMiniMap();
            keyPressed = "up";
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            SetTarget(180f);
            cameraManager.DisableMiniMap();
            keyPressed = "down";
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            SetTarget(-90f);
            cameraManager.DisableMiniMap();
            keyPressed = "left";
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            SetTarget(90f);
            cameraManager.DisableMiniMap();
            keyPressed = "right";
        }

        if (!string.IsNullOrEmpty(keyPressed))
            LogData(new System.Collections.Generic.Dictionary<string, object>
            {
                {"event_type", "key_press"},
                {"key_pressed", keyPressed},
                {"t_curr_run", keyPressTime},
                {"curr_loc_x", oldPosition.x},
                {"curr_loc_y", oldPosition.z}
            });
    }

    void SetTarget(float relativeYRotation) //V: calculate rotation target relative to current position and set isRotating to true
    {
        float currentYRotation = transform.rotation.eulerAngles.y;
        float newYRotation = currentYRotation + relativeYRotation;
        targetRotation = Quaternion.Euler(0, newYRotation, 0);
        isRotating = true;

    }

    void RotateToTarget()
    {
        if (!rotationStartLogged) //V: prevents from logging at each single frame
        {
            LogData(new System.Collections.Generic.Dictionary<string, object>
            {
                {"event_type", "rotation_start"},
                {"from_rotation", transform.rotation.eulerAngles.y},
                {"target_rotation", targetRotation.eulerAngles.y},
                {"t_curr_run", CurrentRunTime()}
            });
            rotationStartLogged = true;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime 
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.01f)
        {
            //V; ensure rotation is 90 degree multiple
            float y = Mathf.Round(targetRotation.eulerAngles.y / 90f) * 90f;
            transform.rotation = Quaternion.Euler(0, y, 0);
            isRotating = false;

            LogData(new System.Collections.Generic.Dictionary<string, object>
            {
                {"event_type", "rotation_complete"},
                {"final_rotation", transform.rotation.eulerAngles.y},
                {"t_curr_run", CurrentRunTime()}
            });
            rotationStartLogged = false;
        }
    }

    bool WithinBounds(Vector3 position) //V: check that we are within grid boundaries
    {
        float leftBound = -5.3f;
        float rightBound = 15.3f;
        float upBound = 25.6f; //V: for upper bounds we use z coordinates
        float bottomBound = 5f;
        float tolerance = 0.1f;

        return position.x > leftBound - tolerance && 
        position.x < rightBound + tolerance && 
        position.z < upBound + tolerance && 
        position.z > bottomBound - tolerance;
    }
    
    void MoveToTarget()
    {
        if (!movementStartLogged)
        {
            LogData(new System.Collections.Generic.Dictionary<string, object>
            {
                {"event_type", "movement_start"},
                {"from_x", transform.position.x},
                {"from_z", transform.position.z},
                {"target_x", targetPosition.x},
                {"target_z", targetPosition.z},
                {"t_curr_run", CurrentRunTime()}
            });
            movementStartLogged = true;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime //V: Time.deltaTime = time since last frame; ensures moving time is constant despite ≠ computers may have ≠ updating speed
        );
        
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f) //V: if the distance between current and target position = 0.01, then snap to target 
        {
            transform.position = targetPosition;
            isMoving = false;

            LogData(new System.Collections.Generic.Dictionary<string, object>
            {
                {"event_type", "movement_complete"},
                {"final_x", transform.position.x},
                {"final_z", transform.position.z},
                {"t_curr_run", CurrentRunTime()}
            });
            movementStartLogged = false;

            rewardManager.RewardFound(transform.position);
        }
    }
}