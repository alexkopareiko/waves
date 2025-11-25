using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MineController : MonoBehaviour
{
    [SerializeField] private float horizontalShift = 1f;
    [SerializeField] private float forwardShift = 1f;

    private Rigidbody _rigidbody;
    private Vector3 _startPosition;
    private float _elapsed;
    private const float LerpRate = 3f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        _elapsed += Time.fixedDeltaTime;
        var horizontalOffset = Mathf.Sin(_elapsed * 0.7f) * horizontalShift;
        var forwardOffset = Mathf.Sin((_elapsed + 1.5f) * 0.5f) * forwardShift;

        // Determine where the mine should be relative to its original spot
        var targetPosition = _startPosition + transform.right * horizontalOffset + transform.forward * forwardOffset;
        var nextPosition = Vector3.Lerp(_rigidbody.position, targetPosition, Time.fixedDeltaTime * LerpRate);
        _rigidbody.MovePosition(nextPosition);
    }
}
