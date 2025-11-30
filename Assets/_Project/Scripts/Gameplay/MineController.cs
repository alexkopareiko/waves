using Bitgem.VFX.StylisedWater;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MineController : MonoBehaviour
{
    [SerializeField] private float shiftLength = 1f;
    [SerializeField] private float speed = 1f;
    [SerializeField] private Vector2 durationRange = new Vector2(1f, 2f);
    [SerializeField] private float delayBetweenMoves = 0.5f;
    [SerializeField] private float verticalLerpSpeed = 1f;
    [SerializeField] private WateverVolumeFloater floater = null;

    private Rigidbody _rigidbody;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _elapsedMove;
    private float _moveDuration;
    private float _delayElapsed;
    private bool _waitingForDelay;
    private bool _shouldDropNextVertical;
    private float _targetVerticalOffset;
    private float _currentVerticalOffset;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _startPosition = transform.position;
        _targetPosition = _startPosition;
        _shouldDropNextVertical = true;
        _targetVerticalOffset = 0f;
        _currentVerticalOffset = 0f;
        floater = floater ?? GetComponent<WateverVolumeFloater>();
        ApplyVerticalOffset();
    }

    private void Start()
    {
        ChooseNextTarget();
    }

    private void Update()
    {
        if (_waitingForDelay)
        {
            _delayElapsed += Time.deltaTime;
            if (_delayElapsed >= delayBetweenMoves)
            {
                _waitingForDelay = false;
                _delayElapsed = 0f;
                ChooseNextTarget();
            }

            return;
        }

        _elapsedMove += Time.deltaTime;
        if (_elapsedMove >= _moveDuration)
        {
            _waitingForDelay = true;
            _elapsedMove = 0f;
        }
    }

    private void FixedUpdate()
    {
        UpdateCurrentVerticalOffset();

        var nextPosition = Vector3.Lerp(_rigidbody.position, _targetPosition, Time.fixedDeltaTime * speed);
        if (floater)
        {
            nextPosition.y = _rigidbody.position.y;
        }
        _rigidbody.MovePosition(nextPosition);
    }

    private void UpdateCurrentVerticalOffset()
    {
        if (!floater)
        {
            return;
        }

        var step = verticalLerpSpeed * Time.fixedDeltaTime;
        _currentVerticalOffset = Mathf.MoveTowards(_currentVerticalOffset, _targetVerticalOffset, step);
        ApplyVerticalOffset();
    }

    private void ChooseNextTarget()
    {
        _elapsedMove = 0f;
        var minDuration = Mathf.Min(durationRange.x, durationRange.y);
        var maxDuration = Mathf.Max(durationRange.x, durationRange.y);
        _moveDuration = Random.Range(minDuration, maxDuration);
        _targetVerticalOffset = DetermineVerticalOffset();
        _targetPosition = CalculateTargetPosition();
    }

    private Vector3 CalculateTargetPosition()
    {
        var directions = new[]
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        var direction = directions[Random.Range(0, directions.Length)];
        var horizontalOffset = direction * Mathf.Max(0f, shiftLength);

        var target = _startPosition + horizontalOffset;
        if (floater == null)
        {
            target.y += _targetVerticalOffset;
        }

        return target;
    }

    private float DetermineVerticalOffset()
    {
        if (floater)
        {
            _shouldDropNextVertical = false;
            return 0f;
        }

        var safeShift = Mathf.Max(0f, shiftLength);
        var upperLimit = _startPosition.y + safeShift;
        var waterHeight = GetWaterHeight();
        if (waterHeight.HasValue)
        {
            upperLimit = Mathf.Min(upperLimit, waterHeight.Value);
        }

        var lowerLimit = _startPosition.y - safeShift;
        if (lowerLimit >= upperLimit)
        {
            _shouldDropNextVertical = false;
            return upperLimit - _startPosition.y;
        }

        if (_shouldDropNextVertical)
        {
            _shouldDropNextVertical = false;
            return lowerLimit - _startPosition.y;
        }

        _shouldDropNextVertical = true;
        return upperLimit - _startPosition.y;
    }

    private void ApplyVerticalOffset()
    {
        if (floater)
        {
            floater.VerticalOffset = _currentVerticalOffset;
        }
    }

    private float? GetWaterHeight()
    {
        var helper = WaterVolumeHelper.Instance;
        if (helper == null)
        {
            return null;
        }

        return helper.GetHeight(transform.position);
    }
}
