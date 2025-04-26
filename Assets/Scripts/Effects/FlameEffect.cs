using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FlameEffect : MonoBehaviour
{
    [Header("Flame Movement")]
    [SerializeField, Range(0.1f, 2f)] private float height = 0.8f;
    [SerializeField, Range(0.1f, 10f)] private float swaySpeed = 3f;
    [SerializeField, Range(0f, 0.5f)] private float swayAmount = 0.15f;
    [SerializeField, Range(0f, 1f)] private float turbulence = 0.3f;

    [Header("Particle Appearance")]
    [SerializeField, Range(0.01f, 0.5f)] private float minSize = 0.1f;
    [SerializeField, Range(0.01f, 0.5f)] private float maxSize = 0.3f;
    [SerializeField] private Gradient sizeOverLifetime;
    [SerializeField] private AnimationCurve sizeCurve = new AnimationCurve(
        new Keyframe(0, 0), 
        new Keyframe(0.3f, 1), 
        new Keyframe(1, 0)
    );

    [Header("Advanced")]
    [SerializeField] private Vector3 windDirection = new Vector3(0.1f, 0, 0);
    [SerializeField] private float windStrength = 0.05f;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private float[] particleOffsets;
    private Vector3[] particleWindEffects;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startSpeed = 0;
        
        InitializeParticleSystem();
    }

    void InitializeParticleSystem()
    {
        var main = ps.main;
        var colorOverLifetime = ps.colorOverLifetime;
        
        main.startLifetime = height * 0.8f;
        colorOverLifetime.color = sizeOverLifetime;
    }

    void LateUpdate()
    {
        InitializeParticleArraysIfNeeded();

        int count = ps.GetParticles(particles);
        
        for (int i = 0; i < count; i++)
        {
            UpdateParticlePosition(ref particles[i], i);
            UpdateParticleSize(ref particles[i]);
            ApplyWindEffect(ref particles[i], i);
        }
        
        ps.SetParticles(particles, count);
    }

    private void InitializeParticleArraysIfNeeded()
    {
        if (particles == null || particles.Length < ps.main.maxParticles)
        {
            particles = new ParticleSystem.Particle[ps.main.maxParticles];
            particleOffsets = new float[ps.main.maxParticles];
            particleWindEffects = new Vector3[ps.main.maxParticles];
            
            for (int i = 0; i < particleOffsets.Length; i++)
            {
                particleOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
                particleWindEffects[i] = new Vector3(
                    Random.Range(-1f, 1f) * windStrength,
                    Random.Range(-0.2f, 0.2f) * windStrength,
                    Random.Range(-1f, 1f) * windStrength
                );
            }
        }
    }

    private void UpdateParticlePosition(ref ParticleSystem.Particle particle, int index)
    {
        float lifetimeProgress = 1 - (particle.remainingLifetime / particle.startLifetime);
        
        float swayX = Mathf.Sin(Time.time * swaySpeed + particleOffsets[index]) * swayAmount;
        float swayZ = Mathf.Cos(Time.time * swaySpeed * 0.7f + particleOffsets[index]) * swayAmount * 0.5f;
        
        float turbulenceOffset = Mathf.PerlinNoise(index * 0.1f, Time.time * 5f) * turbulence;
        
        particle.position = new Vector3(
            swayX * (1 + turbulenceOffset),
            lifetimeProgress * height,
            swayZ * (1 + turbulenceOffset * 0.5f)
        );
    }

    private void UpdateParticleSize(ref ParticleSystem.Particle particle)
    {
        float lifetimeProgress = 1 - (particle.remainingLifetime / particle.startLifetime);
        float sizeProgress = sizeCurve.Evaluate(lifetimeProgress);
        particle.startSize = Mathf.Lerp(minSize, maxSize, sizeProgress);
    }

    private void ApplyWindEffect(ref ParticleSystem.Particle particle, int index)
    {
        float lifetimeProgress = 1 - (particle.remainingLifetime / particle.startLifetime);
        Vector3 windEffect = Vector3.Lerp(
            particleWindEffects[index] * 0.1f,
            particleWindEffects[index] + windDirection * windStrength,
            lifetimeProgress
        );
        
        particle.position += windEffect * Time.deltaTime;
    }
}