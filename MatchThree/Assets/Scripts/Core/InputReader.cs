using Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, InputActions.ITouchscreenActions
{
    [SerializeField] private Camera _mainCam;
    private InputActions _controls;
    private Vector2 _touchPos;
    private Matchable[] _selectedMatchables = new Matchable[2];
    private MatchableGrid _grid;

    private void Awake()
    {
        InitializeControls();
        _grid = (MatchableGrid)MatchableGrid.Instance;
    }

    private void OnEnable()
    {
        if (_controls == null)
        {
            InitializeControls();
        }

        _controls?.Touchscreen.Enable();
    }

    private void OnDisable()
    {
        _controls?.Touchscreen.Disable();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMousePointerDown();
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            HandleMousePointerUp();
        }
    }

    private void InitializeControls()
    {
        _controls = new InputActions();
        _controls.Touchscreen.SetCallbacks(this);
    }

    private void HandleMousePointerDown()
    {
        _touchPos = Input.mousePosition;
        HandleOnTouchedDown();
    }

    private void HandleMousePointerUp()
    {
        _touchPos = Input.mousePosition;
        HandleOnTouchedUp();
    }

    public void OnTouchPosition(InputAction.CallbackContext context)
    {
        if (context.performed)
            _touchPos = context.ReadValue<Vector2>();
    }

    public void OnTouchPress(InputAction.CallbackContext context)
    {
        if (context.performed)
            HandleOnTouchedDown();
        if (context.canceled)
            HandleOnTouchedUp();
    }

    private void HandleOnTouchedDown()
    {
        var camera = _mainCam != null ? _mainCam : Camera.main;
        if (camera == null)
            return;

        Vector2 worldPoint = camera.ScreenToWorldPoint(_touchPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
        if (hit.collider != null)
        {
            if (hit.collider.TryGetComponent(out Matchable matchable))
            {
                if (!matchable.IsMoving)
                {
                    _selectedMatchables[0] = matchable;
                    _selectedMatchables[0].GetSelected();
                }
            }
        }
    }

    private void HandleOnTouchedUp()
    {
        var camera = _mainCam != null ? _mainCam : Camera.main;
        if (camera == null)
            return;

        Vector2 worldPoint = camera.ScreenToWorldPoint(_touchPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider != null)
        {
            if (hit.collider.TryGetComponent(out Matchable matchable))
            {
                if (matchable != _selectedMatchables[0] && !matchable.IsMoving)
                    _selectedMatchables[1] = matchable;
            }
        }

        if (_selectedMatchables[0] != null && _selectedMatchables[1] != null)
        {
            if (_grid.AreAdjacents(_selectedMatchables[0], _selectedMatchables[1]))
            {
                StartCoroutine(_grid.TryMatch(_selectedMatchables[0], _selectedMatchables[1]));
            }
            else
            {
                int x = _selectedMatchables[0].GridPosition.x;
                int y = _selectedMatchables[0].GridPosition.y;

                int selectedX = _selectedMatchables[1].GridPosition.x;
                int selectedY = _selectedMatchables[1].GridPosition.y;

                if (selectedX > x)
                {
                    if (selectedY == y)
                    {
                        StartCoroutine(_grid.TryMatch(_selectedMatchables[0], _grid.GetItemAt(x + 1, selectedY)));
                    }
                }
                else if (selectedX < x)
                {
                    if (selectedY == y)
                    {
                        StartCoroutine(_grid.TryMatch(_selectedMatchables[0], _grid.GetItemAt(x - 1, selectedY)));
                    }
                }
                else
                {
                    if (selectedY > y)
                    {
                        StartCoroutine(_grid.TryMatch(_selectedMatchables[0], _grid.GetItemAt(x, y + 1)));
                    }
                    else
                    {
                        StartCoroutine(_grid.TryMatch(_selectedMatchables[0], _grid.GetItemAt(x, y - 1)));
                    }
                }
            }
        }

        _selectedMatchables[0]?.GetUnselected();
        _selectedMatchables[0] = _selectedMatchables[1] = null;
    }
}
