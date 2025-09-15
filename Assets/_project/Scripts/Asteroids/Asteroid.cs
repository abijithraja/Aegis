using UnityEngine;

public class Asteroid : MonoBehaviour, IDamageable
{
    // --- Existing Variables ---
    [SerializeField] private FracturedAsteroid _fracturedAsteroidPrefab;
    [SerializeField] private Detonator _explosionPrefab;

    // --- Movement Variables ---
    [Header("Initial Movement")]
    [Tooltip("The minimum speed the asteroid will move at.")]
    [SerializeField] private float minSpeed = 5f;
    [Tooltip("The maximum speed the asteroid will move at.")]
    [SerializeField] private float maxSpeed = 15f;
    [Tooltip("How fast the asteroid will tumble.")]
    [SerializeField] private float rotationSpeed = 5f;

    // --- Wander Variables ---
    [Header("Wander Behavior")]
    [Tooltip("How strong the random side-to-side force is. Set to 0 for no wander.")]
    [SerializeField] private float wanderStrength = 1.5f;
    [Tooltip("How frequently the random wander direction changes.")]
    [SerializeField] private float wanderFrequency = 2f;


    // --- Private Variables ---
    private Transform _transform;
    private Rigidbody rb;
    private float perlinSeed; // A unique seed for this asteroid's wander pattern

    // --- Chase Variables ---
    private bool isChasing = false;
    private Transform chaseTarget;
    private float chaseSpeed;

    private void Awake()
    {
        _transform = transform;
        rb = GetComponent<Rigidbody>();
        // Give each asteroid a unique random seed for its wander pattern
        perlinSeed = Random.Range(-1000f, 1000f);
    }

    private void Start()
    {
        if (!isChasing && rb != null)
        {
            float speed = Random.Range(minSpeed, maxSpeed);
            rb.AddForce(transform.forward * speed, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * rotationSpeed);
        }
    }

    private void FixedUpdate()
    {
        if (isChasing)
        {
            // If we are in chase mode, constantly update our velocity to move towards the target.
            if (chaseTarget != null && rb != null)
            {
                Vector3 direction = (chaseTarget.position - _transform.position).normalized;
                rb.velocity = direction * chaseSpeed;
            }
        }
        else if (rb != null && wanderStrength > 0)
        {
            // If not chasing, apply a gentle, wandering force using smooth Perlin noise.
            // This creates a nice drifting effect instead of jerky random movement.
            float noiseX = Mathf.PerlinNoise(Time.time * wanderFrequency, perlinSeed) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(perlinSeed, Time.time * wanderFrequency) * 2f - 1f;

            Vector3 wanderForce = new Vector3(noiseX, noiseY, 0) * wanderStrength;
            rb.AddForce(wanderForce);
        }
    }

    // --- Public Methods & Properties ---

    /// <summary>
    /// A public, read-only property to check if the asteroid is in chase mode.
    /// </summary>
    public bool IsChasing => isChasing;

    public void StartChaseMode(Transform target, float speed)
    {
        isChasing = true;
        chaseTarget = target;
        chaseSpeed = speed;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void TakeDamage(int damage, Vector3 hitPosition)
    {
        FractureAsteroid(hitPosition);
    }

    private void FractureAsteroid(Vector3 hitPosition)
    {
        if (_fracturedAsteroidPrefab != null)
        {
            Instantiate(_fracturedAsteroidPrefab, _transform.position, _transform.rotation);
        }
        if (_explosionPrefab != null)
        {
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}