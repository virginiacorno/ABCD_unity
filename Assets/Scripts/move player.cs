using UnityEngine;
using UnityEngine.InputSystem; 
public class moveplayer : MonoBehaviour
{
    private float _rotationFrom;

    public float gridStepSize = 10.3f;
    public float moveSpeed = 7.0f; //V: modify later, should go back to 3f to ensure 2 TRs in shortest path
    public float rotationSpeed = 100f;

    public rewardManager rewardManager;
    
    [SerializeField] private MonoBehaviour _cameraController;
    public ICameraController CameraController => _cameraController as ICameraController;

    public bool inputEnabled = true; //V: allows to detect key input, turned off at the end of trials when transition screens/resets are called

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isMoving = false;
    private bool isRotating = false;
    

    //V: step count for feedback
    public int stepCount = 0;

    //V: time variables for logging
    private float _tStepPressGlobal;
    private Vector3 _positionAtPress;
    private float _tStepPressCurrRun;
    private float _rotationPressGlobal;
    private float _rotationPressCurrRun;
    private float repStartTime;
    
    void Start()
    {
        targetPosition = transform.position;
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
        transform.rotation = Quaternion.identity; //V: reset to initial facing direction (forward along +Z)
        targetRotation = Quaternion.identity;
        isMoving = false;
        isRotating = false;

    }
    
    void CheckInput() //V: check keyboard input and set the rotation and movement targets accordingly
    {
        if (!inputEnabled) return; //V: early return if input is disabled

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;  // Safety check

        string keyPressed = null;
        Vector3 oldPosition = transform.position;

        if (keyboard.upArrowKey.wasPressedThisFrame) //V: up key is the only one allowing to move, the other ones are just controlling rotations
        {
            _tStepPressGlobal = Time.time - TRPulse.Instance.t0; //V: time since experiment started (i.e., t0 detected)
            _tStepPressCurrRun = Time.time - repStartTime;
            _positionAtPress = transform.position;

            Vector3 potentialTarget = transform.position + (transform.forward * gridStepSize);
            if (WithinBounds(potentialTarget))
            {
                targetPosition = potentialTarget;
                isMoving = true;
                stepCount ++;
            }
            CameraController.DisableMiniMap();
            keyPressed = "up";
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            _rotationPressGlobal = Time.time - TRPulse.Instance.t0;
            _rotationPressCurrRun = Time.time - repStartTime;

            SetTarget(180f);
            CameraController.DisableMiniMap();
            keyPressed = "down";
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            _rotationPressGlobal = Time.time - TRPulse.Instance.t0;
            _rotationPressCurrRun = Time.time - repStartTime;

            SetTarget(-90f);
            CameraController.DisableMiniMap();
            keyPressed = "left";
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            _rotationPressGlobal = Time.time - TRPulse.Instance.t0;
            _rotationPressCurrRun = Time.time - repStartTime;

            SetTarget(90f);
            CameraController.DisableMiniMap();
            keyPressed = "right";
        }

    }

    void SetTarget(float relativeYRotation) //V: calculate rotation target relative to current position and set isRotating to true
    {
        _rotationFrom = transform.rotation.eulerAngles.y;
        float newYRotation = _rotationFrom + relativeYRotation;
        targetRotation = Quaternion.Euler(0, newYRotation, 0);
        isRotating = true;
    }

    void RotateToTarget()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.01f)
        {
            float y = Mathf.Round(targetRotation.eulerAngles.y / 90f) * 90f;
            transform.rotation = Quaternion.Euler(0, y, 0);
            isRotating = false;

            Vector3 rewPos = rewardManager.GetCurrentRewardPosition();
            DataLogger.Instance.LogRotation(
                _rotationPressGlobal, _rotationPressCurrRun,
                _rotationFrom, transform.rotation.eulerAngles.y,
                transform.position.x, transform.position.z,
                rewPos.x, rewPos.z,
                rewardManager.GetCurrentState(),
                rewardManager.config.IsBackw ? "backw" : "forw",
                rewardManager.repsCompleted,
                rewardManager.GetCurrentConfigName()
            );
        }
    }

    bool WithinBounds(Vector3 position) //V: check that we are within grid boundaries
    {
        float leftBound = -5.3f;
        float rightBound = 35.9f;
        float upBound = 35.9f; //V: for upper bounds we use z coordinates
        float bottomBound = -5.3f;
        float tolerance = 0.1f;

        return position.x > leftBound - tolerance && 
        position.x < rightBound + tolerance && 
        position.z < upBound + tolerance && 
        position.z > bottomBound - tolerance;
    }
    
    void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;

            float tStepEndGlobal = Time.time - TRPulse.Instance.t0;
            Vector3 rewPos = rewardManager.GetCurrentRewardPosition();

            DataLogger.Instance.LogStep(
                _tStepPressGlobal, _tStepPressCurrRun,
                0f,
                _positionAtPress.x, _positionAtPress.z,
                transform.position.x, transform.position.z,
                tStepEndGlobal,
                rewPos.x, rewPos.z,
                rewardManager.GetCurrentState(),
                rewardManager.config.IsBackw ? "backw" : "forw",
                rewardManager.repsCompleted,
                rewardManager.GetCurrentConfigName()
            );

            rewardManager.RewardFound(transform.position);
        }
    }
}