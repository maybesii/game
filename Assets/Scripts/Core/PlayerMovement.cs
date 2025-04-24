using UnityEngine;
using HorrorGame;

public class PlayerMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
    
    [Header("Настройки прыжка")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float airControlMultiplier = 0.5f;
    
    [Header("Настройки обзора")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Vector2 verticalLookLimits = new Vector2(-90f, 90f);
    [SerializeField] private Camera playerCamera;
    
    [Header("Настройки покачивания головы")]
    [SerializeField] private AnimationCurve bobCurveY = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve bobCurveX = AnimationCurve.EaseInOut(0, 0, 1, 0.5f);
    [SerializeField] private float bobFrequencyWalk = 1.8f;
    [SerializeField] private float bobFrequencyRun = 2.5f;
    [SerializeField] private float bobAmplitudeY = 0.15f;
    [SerializeField] private float bobAmplitudeX = 0.05f;
    [SerializeField] private float runAmplitudeMultiplier = 1.4f;
    [SerializeField] private float strafeAmplitudeMultiplier = 0.7f;
    [SerializeField] private float noiseAmplitude = 0.03f;
    [SerializeField] private float noiseFrequency = 4f;
    [SerializeField] private float bobSpeedThreshold = 0.01f;
    [SerializeField] private float returnSpeed = 8f;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private float strafeTransitionSpeed = 8f;
    [SerializeField] private float inputSmoothSpeed = 10f;

    [Header("Настройки покачивания при прыжке")]
    [SerializeField] private AnimationCurve jumpBobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float jumpBobAmplitude = 0.1f;
    [SerializeField] private float landBobAmplitude = 0.15f;
    [SerializeField] private float jumpBobDuration = 0.3f;
    [SerializeField] private float landBobDuration = 0.4f;

    [Header("Настройки наклона камеры")]
    [SerializeField] private float tiltAngle = 7f;
    [SerializeField] private float tiltSpeed = 10f;
    [SerializeField] private float tiltAmplitudeMultiplier = 1.3f;

    [Header("Эффекты ужаса")]
    [SerializeField] private float fearMultiplier = 1f;

    [Header("Настройки гравитации")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Настройки звука шагов")]
    [SerializeField] private AudioClip puddleStepClip;
    [SerializeField] private AudioClip groundStepClip;
    [SerializeField] private AudioClip puddleSplashClip;  
    [SerializeField] [Range(0f, 1f)] private float maxStepVolume = 0.5f;
    [SerializeField] private WeatherController weatherController;
    [SerializeField] [Range(0.5f, 1.5f)] private float walkPitch = 0.8f;
    [SerializeField] [Range(0.5f, 1.5f)] private float runPitch = 1.2f;
    [SerializeField] private float volumeTransitionSpeed = 5f;
    [SerializeField] private float pitchTransitionSpeed = 5f;

    [Header("Взаимодействие с лужами")]
    [SerializeField] private ParticleSystem splashEffect; 
    [SerializeField] private float puddleCheckRadius = 0.5f; 
    [SerializeField] private LayerMask puddleLayer;
    [SerializeField] [Range(0f, 1f)] private float maxSplashVolume = 0.3f;

    [Header("Настройки звука стука в окно")]
    [SerializeField] private AudioClip winKnockClip;
    [SerializeField] [Range(0f, 1f)] private float maxWinKnockVolume = 0.5f;
    [SerializeField] private float soundPositionOffset = 2f;

    private CharacterController controller;
    private float xRotation;
    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private float bobTimer;
    private Vector3 initialCameraPosition;
    private float noiseSeedX, noiseSeedY;
    private float currentBobAmplitudeY, currentBobAmplitudeX;
    private float currentBobFrequency;
    private float targetBobAmplitudeY, targetBobAmplitudeX;
    private float targetBobFrequency;
    private float jumpBobTimer;
    private float landBobTimer;
    private bool isJumping;
    private bool isLanding;
    private float smoothedHorizontalInput;
    private AudioSource puddleAudioSource;
    private AudioSource groundAudioSource;
    private AudioSource splashAudioSource;
    private AudioSource winKnockAudioSource;
    private float currentPuddleVolume;
    private float currentGroundVolume;
    private float currentPitch;
    private bool isWinKnockTriggered;
    private float lastSplashTime;
    private float splashCooldown = 0.3f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                enabled = false;
                return;
            }
        }

        initialCameraPosition = playerCamera.transform.localPosition;
        noiseSeedX = Random.Range(0f, 1000f);
        noiseSeedY = Random.Range(0f, 1000f);

        puddleAudioSource = gameObject.AddComponent<AudioSource>();
        puddleAudioSource.clip = puddleStepClip;
        puddleAudioSource.volume = 0f;
        puddleAudioSource.pitch = 1f;
        puddleAudioSource.loop = true;
        puddleAudioSource.spatialBlend = 0f;
        puddleAudioSource.playOnAwake = false;

        groundAudioSource = gameObject.AddComponent<AudioSource>();
        groundAudioSource.clip = groundStepClip;
        groundAudioSource.volume = 0f;
        groundAudioSource.pitch = 1f;
        groundAudioSource.loop = true;
        groundAudioSource.spatialBlend = 0f;
        groundAudioSource.playOnAwake = false;

        splashAudioSource = gameObject.AddComponent<AudioSource>();
        splashAudioSource.clip = puddleSplashClip;
        splashAudioSource.volume = 0f;
        splashAudioSource.loop = false;
        splashAudioSource.spatialBlend = 1f;
        splashAudioSource.playOnAwake = false;
        splashAudioSource.spread = 180f;
        splashAudioSource.minDistance = 1f;
        splashAudioSource.maxDistance = 10f;

        winKnockAudioSource = gameObject.AddComponent<AudioSource>();
        winKnockAudioSource.clip = winKnockClip;
        winKnockAudioSource.volume = 0f;
        winKnockAudioSource.loop = false;
        winKnockAudioSource.spatialBlend = 1f;
        winKnockAudioSource.playOnAwake = false;
        winKnockAudioSource.spread = 180f;
        winKnockAudioSource.minDistance = 1f;
        winKnockAudioSource.maxDistance = 20f;

        if (!weatherController)
        {
            weatherController = FindObjectOfType<WeatherController>();
        }

        gameObject.tag = "Player";
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleCeilingCheck();
        HandleLook();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        HandleHeadBob();
        HandleCameraTilt();
        HandleStepSound();
        HandlePuddleInteraction();
        HandleWinKnockSound();
    }

    private void HandleGroundCheck()
    {
        wasGroundedLastFrame = isGrounded;
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = groundedGravity;

        if (!wasGroundedLastFrame && isGrounded)
        {
            isLanding = true;
            landBobTimer = 0f;
        }
    }

    private void HandleCeilingCheck()
    {
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && velocity.y > 0)
        {
            velocity.y = 0;
        }
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, verticalLookLimits.x, verticalLookLimits.y);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, playerCamera.transform.localRotation.eulerAngles.z);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        float speed = Input.GetKey(runKey) ? runSpeed : walkSpeed;
        if (!isGrounded) speed *= airControlMultiplier;

        controller.Move(move * speed * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            isJumping = true;
            jumpBobTimer = 0f;
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleHeadBob()
    {
        float rawHorizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        smoothedHorizontalInput = Mathf.Lerp(smoothedHorizontalInput, rawHorizontalInput, Time.deltaTime * inputSmoothSpeed);
        float movementSpeed = new Vector2(smoothedHorizontalInput, verticalInput).magnitude;

        Vector3 bobOffset = Vector3.zero;

        if (isJumping)
        {
            jumpBobTimer += Time.deltaTime / jumpBobDuration;
            if (jumpBobTimer <= 1f)
            {
                float jumpOffset = jumpBobCurve.Evaluate(jumpBobTimer) * jumpBobAmplitude;
                bobOffset += new Vector3(0, -jumpOffset, 0);
            }
            else
            {
                isJumping = false;
            }
        }

        if (isLanding)
        {
            landBobTimer += Time.deltaTime / landBobDuration;
            if (landBobTimer <= 1f)
            {
                float landOffset = jumpBobCurve.Evaluate(landBobTimer) * landBobAmplitude;
                bobOffset += new Vector3(0, -landOffset, 0);
            }
            else
            {
                isLanding = false;
            }
        }

        if (isGrounded && movementSpeed > bobSpeedThreshold && !isJumping && !isLanding)
        {
            bool isRunning = Input.GetKey(runKey);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            float speedFactor = currentSpeed / walkSpeed;

            float strafeFactor = Mathf.Abs(smoothedHorizontalInput) > Mathf.Abs(verticalInput) ? strafeAmplitudeMultiplier : 1f;

            targetBobFrequency = isRunning ? bobFrequencyRun : bobFrequencyWalk;
            targetBobAmplitudeY = bobAmplitudeY * speedFactor * (isRunning ? runAmplitudeMultiplier : 1f) * strafeFactor;
            targetBobAmplitudeX = bobAmplitudeX * speedFactor * (isRunning ? runAmplitudeMultiplier : 1f) * strafeFactor;

            float currentTransitionSpeed = Mathf.Abs(smoothedHorizontalInput) > 0.1f ? strafeTransitionSpeed : transitionSpeed;
            currentBobFrequency = Mathf.Lerp(currentBobFrequency, targetBobFrequency, Time.deltaTime * currentTransitionSpeed);
            currentBobAmplitudeY = Mathf.Lerp(currentBobAmplitudeY, targetBobAmplitudeY, Time.deltaTime * currentTransitionSpeed);
            currentBobAmplitudeX = Mathf.Lerp(currentBobAmplitudeX, targetBobAmplitudeX, Time.deltaTime * currentTransitionSpeed);

            bobTimer += Time.deltaTime * currentBobFrequency;
            float bobOffsetY = bobCurveY.Evaluate(Mathf.PingPong(bobTimer, 1f)) * currentBobAmplitudeY;
            float bobOffsetX = bobCurveX.Evaluate(Mathf.PingPong(bobTimer, 1f)) * currentBobAmplitudeX;

            bobOffset += new Vector3(bobOffsetX, bobOffsetY, 0);

            float noiseScale = movementSpeed * fearMultiplier;
            float noiseOffsetY = (Mathf.PerlinNoise(Time.time * noiseFrequency * fearMultiplier + noiseSeedY, 0) - 0.5f) * noiseAmplitude * noiseScale;
            float noiseOffsetX = (Mathf.PerlinNoise(Time.time * noiseFrequency * fearMultiplier + noiseSeedX, 0) - 0.5f) * noiseAmplitude * noiseScale;
            bobOffset += new Vector3(noiseOffsetX, noiseOffsetY, 0);
        }
        else if (!isJumping && !isLanding)
        {
            bobTimer = 0;
            currentBobAmplitudeY = Mathf.Lerp(currentBobAmplitudeY, 0, Time.deltaTime * returnSpeed);
            currentBobAmplitudeX = Mathf.Lerp(currentBobAmplitudeX, 0, Time.deltaTime * returnSpeed);
        }

        Vector3 newPosition = initialCameraPosition + bobOffset;
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, newPosition, Time.deltaTime * returnSpeed);
    }

    private void HandleCameraTilt()
    {
        float speedFactor = Input.GetKey(runKey) ? tiltAmplitudeMultiplier : 1f;
        float targetTilt = -smoothedHorizontalInput * tiltAngle * speedFactor;

        float currentTilt = playerCamera.transform.localRotation.eulerAngles.z;
        if (currentTilt > 180) currentTilt -= 360;
        float newTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, newTilt);
    }

    private void HandleStepSound()
    {
        float movementSpeed = new Vector2(smoothedHorizontalInput, Input.GetAxisRaw("Vertical")).magnitude;
        if (!isGrounded || movementSpeed <= bobSpeedThreshold || isJumping || isLanding)
        {
            currentPuddleVolume = Mathf.Lerp(currentPuddleVolume, 0f, Time.deltaTime * volumeTransitionSpeed);
            currentGroundVolume = Mathf.Lerp(currentGroundVolume, 0f, Time.deltaTime * volumeTransitionSpeed);
            puddleAudioSource.volume = currentPuddleVolume;
            groundAudioSource.volume = currentGroundVolume;
            if (currentPuddleVolume < 0.01f && puddleAudioSource.isPlaying)
            {
                puddleAudioSource.Stop();
            }
            if (currentGroundVolume < 0.01f && groundAudioSource.isPlaying)
            {
                groundAudioSource.Stop();
            }
            return;
        }

        bool isRaining = weatherController && weatherController.GetCurrentWeatherType() == WeatherZone.WeatherType.Rain;
        float targetVolume = maxStepVolume * movementSpeed * (Input.GetKey(runKey) ? runAmplitudeMultiplier : 1f);
        float targetPitch = Input.GetKey(runKey) ? runPitch : walkPitch;

        if (isRaining)
        {
            currentPuddleVolume = Mathf.Lerp(currentPuddleVolume, targetVolume, Time.deltaTime * volumeTransitionSpeed);
            currentGroundVolume = Mathf.Lerp(currentGroundVolume, 0f, Time.deltaTime * volumeTransitionSpeed);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * pitchTransitionSpeed);

            puddleAudioSource.volume = currentPuddleVolume;
            puddleAudioSource.pitch = currentPitch;
            groundAudioSource.volume = currentGroundVolume;

            if (!puddleAudioSource.isPlaying)
            {
                puddleAudioSource.Play();
            }
            if (currentGroundVolume < 0.01f && groundAudioSource.isPlaying)
            {
                groundAudioSource.Stop();
            }
        }
        else
        {
            currentGroundVolume = Mathf.Lerp(currentGroundVolume, targetVolume, Time.deltaTime * volumeTransitionSpeed);
            currentPuddleVolume = Mathf.Lerp(currentPuddleVolume, 0f, Time.deltaTime * volumeTransitionSpeed);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * pitchTransitionSpeed);

            groundAudioSource.volume = currentGroundVolume;
            groundAudioSource.pitch = currentPitch;
            puddleAudioSource.volume = currentPuddleVolume;

            if (!groundAudioSource.isPlaying)
            {
                groundAudioSource.Play();
            }
            if (currentPuddleVolume < 0.01f && puddleAudioSource.isPlaying)
            {
                puddleAudioSource.Stop();
            }
        }
    }

    private void HandlePuddleInteraction()
    {
        if (!isGrounded || isJumping || isLanding || Time.time - lastSplashTime < splashCooldown)
            return;

        bool isRaining = weatherController && weatherController.GetCurrentWeatherType() == WeatherZone.WeatherType.Rain;
        if (!isRaining) return;

        float movementSpeed = new Vector2(smoothedHorizontalInput, Input.GetAxisRaw("Vertical")).magnitude;
        if (movementSpeed <= bobSpeedThreshold) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, puddleCheckRadius, puddleLayer);
        if (hits.Length > 0)
        {
            lastSplashTime = Time.time;

            splashAudioSource.volume = maxSplashVolume * movementSpeed * (Input.GetKey(runKey) ? runAmplitudeMultiplier : 1f);
            splashAudioSource.Play();

            if (splashEffect)
            {
                splashEffect.transform.position = transform.position + Vector3.up * 0.1f;
                splashEffect.Play();
            }
        }
    }

    private void HandleWinKnockSound()
    {
        if (!winKnockAudioSource || !winKnockClip)
        {
            return;
        }

        if (winKnockAudioSource.isPlaying)
        {
            return;
        }

        if (isWinKnockTriggered)
        {
            winKnockAudioSource.transform.position = transform.position + transform.right * soundPositionOffset;
            winKnockAudioSource.volume = maxWinKnockVolume;
            winKnockAudioSource.Play();
            isWinKnockTriggered = false;
        }
    }

    public void TriggerHorrorSound(HorrorZone.HorrorEffect effect, float intensity)
    {
        if (effect == HorrorZone.HorrorEffect.RiseHorrorSound && !winKnockAudioSource.isPlaying)
        {
            isWinKnockTriggered = true;
            maxWinKnockVolume = maxWinKnockVolume * intensity;
        }
    }

    public void SetFearMultiplier(float value)
    {
        fearMultiplier = Mathf.Clamp(value, 0.5f, 3f);
    }

    public KeyCode GetRunKey() => runKey;
    public float GetFearMultiplier() => fearMultiplier;
}