using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Contracts.Presentation;
using Game.Foundation.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The match-3 board on screen. <see cref="Render"/> draws any board state directly (one gem image per cell); the
    /// <c>Play*</c> methods replay what a <see cref="BoardResolution"/> did (swap, pops, falls, new gems dropping in, shuffle)
    /// and always end by rendering the final board, so a skipped, interrupted or switched-off animation leaves the same
    /// picture. Input: drag a gem towards a neighbour, or click one gem then a neighbour (<see cref="SwapRequested"/>);
    /// while targeting, a click picks a cell (<see cref="CellPicked"/>). The mask dims the board while effects play.
    /// REFERENCES: <c>_area</c> holds the cells and gems (clipped); <c>_cellTemplate</c> and <c>_gemTemplate</c> are inactive
    /// templates; <c>_selection</c> frames the chosen gem; <c>_mask</c> dims the board; <c>_targetHint</c> shows while a
    /// target is being picked.
    /// </summary>
    internal sealed class BoardView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float SwapSeconds = 0.16f;
        private const float PopSeconds = 0.22f;
        private const float FallSecondsPerRow = 0.06f;
        private const float MinFallSeconds = 0.16f;
        private const float ShuffleSeconds = 0.45f;
        private const float MaskSeconds = 0.18f;
        private const float MaskAlpha = 1f;

        [SerializeField] private RectTransform _area;
        [SerializeField] private Image _cellTemplate;
        [SerializeField] private Image _gemTemplate;
        [SerializeField] private RectTransform _selection;
        [SerializeField] private CanvasGroup _mask;
        [SerializeField] private GameObject _targetHint;

        private readonly List<Image> _pool = new List<Image>();
        private Image[,] _grid = new Image[0, 0];
        private Sprite[] _sprites = new Sprite[0];
        private IAnimationSettings _animations;
        private int _rows;
        private int _columns;
        private float _cellSize;
        private bool _hasSelection;
        private Cell _selected;
        private bool _dragging;
        private Cell _dragStart;
        private Vector2 _dragOrigin;
        private bool _targeting;

        /// <summary>The player asked to swap two neighbouring gems.</summary>
        internal event Action<Cell, Cell> SwapRequested;

        /// <summary>The player picked a cell while targeting.</summary>
        internal event Action<Cell> CellPicked;

        /// <summary>A cascade step's gems popped (for sounds and the combo counter).</summary>
        internal event Action<CascadeStep> StepPopped;

        /// <summary>When false, pointer input is ignored (effects playing, paused, run over).</summary>
        internal bool AcceptInput { get; set; }

        internal float CellSize => _cellSize;

        /// <summary>Lays out the cells for a board size and remembers the gem pictures (index = gem kind).</summary>
        internal void Build(int rows, int columns, Sprite[] gemSprites, IAnimationSettings animations)
        {
            _rows = rows;
            _columns = columns;
            _sprites = gemSprites;
            _animations = animations;
            Rect rect = _area.rect;
            _cellSize = Mathf.Floor(Mathf.Min(rect.width / columns, rect.height / rows));
            _grid = new Image[rows, columns];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Image cell = Instantiate(_cellTemplate, _area);
                    cell.gameObject.SetActive(true);
                    cell.rectTransform.sizeDelta = new Vector2(_cellSize, _cellSize);
                    cell.rectTransform.anchoredPosition = Position(new Cell(row, column));
                    cell.transform.SetAsFirstSibling();
                }
            }

            _selection.sizeDelta = new Vector2(_cellSize + 6f, _cellSize + 6f);
            _selection.SetAsLastSibling();
            ClearSelection();
            SetTargeting(false);
            SetMasked(false, false);
        }

        /// <summary>Draws <paramref name="board"/> exactly: every gem at its cell, full size and opaque.</summary>
        internal void Render(Board board)
        {
            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    var cell = new Cell(row, column);
                    int kind = board[cell];
                    Image view = _grid[row, column];
                    if (kind == Board.Empty)
                    {
                        Release(view);
                        _grid[row, column] = null;
                        continue;
                    }

                    if (view == null)
                    {
                        view = Take();
                        _grid[row, column] = view;
                    }

                    Place(view, cell, kind);
                }
            }

            foreach (Image view in _pool)
            {
                if (view.gameObject.activeSelf && !InGrid(view))
                {
                    Release(view);
                }
            }
        }

        /// <summary>A refused swap: the gems slide towards each other and back.</summary>
        internal async Awaitable PlayInvalidSwap(Cell a, Cell b, Board board)
        {
            ClearSelection();
            Image first = Grid(a);
            Image second = Grid(b);
            if (Animate && first != null && second != null)
            {
                Sequence sequence = DOTween.Sequence().SetTarget(this)
                    .Append(first.rectTransform.DOAnchorPos(Position(b), SwapSeconds))
                    .Join(second.rectTransform.DOAnchorPos(Position(a), SwapSeconds))
                    .Append(first.rectTransform.DOAnchorPos(Position(a), SwapSeconds))
                    .Join(second.rectTransform.DOAnchorPos(Position(b), SwapSeconds))
                    .SetLink(gameObject)
                    .Gate(_animations);
                await sequence.AsAwaitable();
            }

            Render(board);
        }

        /// <summary>Replays a resolution step by step, then draws <paramref name="board"/> (the final state).</summary>
        internal async Awaitable PlayResolution(BoardResolution resolution, Board board)
        {
            ClearSelection();
            if (!Animate || resolution == null || !resolution.Valid)
            {
                if (resolution != null)
                {
                    foreach (CascadeStep step in resolution.Steps)
                    {
                        StepPopped?.Invoke(step);
                    }
                }

                Render(board);
                return;
            }

            if (resolution.Swapped)
            {
                Image first = Grid(resolution.A);
                Image second = Grid(resolution.B);
                if (first != null && second != null)
                {
                    _grid[resolution.A.Row, resolution.A.Column] = second;
                    _grid[resolution.B.Row, resolution.B.Column] = first;
                    await Run(DOTween.Sequence()
                        .Append(first.rectTransform.DOAnchorPos(Position(resolution.B), SwapSeconds).SetEase(Ease.OutQuad))
                        .Join(second.rectTransform.DOAnchorPos(Position(resolution.A), SwapSeconds).SetEase(Ease.OutQuad)));
                }
            }

            foreach (CascadeStep step in resolution.Steps)
            {
                if (!Animate)
                {
                    break;
                }

                await PopAsync(step);
                await FallAsync(step);
            }

            if (Animate && resolution.Reshuffled)
            {
                await ShuffleAsync(resolution, board);
            }

            Render(board);
        }

        /// <summary>Dims the board and blocks it while effects play; instant when animations are off.</summary>
        internal void SetMasked(bool masked, bool animate = true)
        {
            DOTween.Kill(_mask);
            _mask.blocksRaycasts = masked;
            _mask.gameObject.SetActive(true);
            float target = masked ? MaskAlpha : 0f;
            if (!animate || !Animate)
            {
                _mask.alpha = target;
                return;
            }

            _mask.DOFade(target, MaskSeconds).SetTarget(_mask).SetLink(gameObject).Gate(_animations);
        }

        /// <summary>Targeting mode: the next click on the board picks a cell instead of selecting a gem.</summary>
        internal void SetTargeting(bool targeting)
        {
            _targeting = targeting;
            _targetHint.SetActive(targeting);
            ClearSelection();
        }

        /// <summary>Screen point of a cell's centre (for orbs flying from the board).</summary>
        internal Vector3 ScreenPoint(Cell cell)
        {
            Vector3 world = _area.TransformPoint(Position(cell));
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }

        /// <summary>Stops every board animation where it is (the next render puts things right).</summary>
        internal void StopAnimations()
        {
            DOTween.Kill(this);
            foreach (Image view in _pool)
            {
                DOTween.Kill(view.rectTransform);
                DOTween.Kill(view);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!AcceptInput || eventData.button != PointerEventData.InputButton.Left || !TryCellAt(eventData, out Cell cell))
            {
                return;
            }

            if (_targeting)
            {
                CellPicked?.Invoke(cell);
                return;
            }

            if (_hasSelection && _selected.IsAdjacent(cell))
            {
                Cell from = _selected;
                ClearSelection();
                SwapRequested?.Invoke(from, cell);
                return;
            }

            Select(cell);
            _dragging = true;
            _dragStart = cell;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out _dragOrigin);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || !AcceptInput || _targeting)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera, out Vector2 point);
            Vector2 delta = point - _dragOrigin;
            if (delta.magnitude < _cellSize * 0.4f)
            {
                return;
            }

            _dragging = false;
            Cell to = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? new Cell(_dragStart.Row, _dragStart.Column + (delta.x > 0f ? 1 : -1))
                : new Cell(_dragStart.Row + (delta.y > 0f ? 1 : -1), _dragStart.Column);
            if (to.Row >= 0 && to.Row < _rows && to.Column >= 0 && to.Column < _columns)
            {
                ClearSelection();
                SwapRequested?.Invoke(_dragStart, to);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragging = false;
        }

        private bool Animate => _animations == null || _animations.Enabled;

        private async Awaitable PopAsync(CascadeStep step)
        {
            StepPopped?.Invoke(step);
            Sequence pops = DOTween.Sequence();
            foreach (GemGroup group in step.Cleared)
            {
                foreach (Cell cell in group.Cells)
                {
                    Image view = Grid(cell);
                    if (view == null)
                    {
                        continue;
                    }

                    _grid[cell.Row, cell.Column] = null;
                    RectTransform rect = view.rectTransform;
                    pops.Join(DOTween.Sequence()
                        .Append(rect.DOScale(1.35f, PopSeconds * 0.45f).SetEase(Ease.OutQuad))
                        .Append(rect.DOScale(0f, PopSeconds * 0.55f).SetEase(Ease.InBack))
                        .Join(view.DOFade(0f, PopSeconds * 0.55f))
                        .OnComplete(() => view.gameObject.SetActive(false)));
                }
            }

            await Run(pops);
        }

        private async Awaitable FallAsync(CascadeStep step)
        {
            Sequence falls = DOTween.Sequence();
            var landed = new List<(Cell cell, Image view)>();
            foreach (GemMove move in step.Moves)
            {
                Image view;
                if (move.IsNew)
                {
                    view = Take();
                    Place(view, move.From, move.Kind);
                }
                else
                {
                    view = Grid(move.From);
                    if (view == null)
                    {
                        continue;
                    }

                    if (_grid[move.From.Row, move.From.Column] == view)
                    {
                        _grid[move.From.Row, move.From.Column] = null;
                    }
                }

                landed.Add((move.To, view));
                float seconds = Mathf.Max(MinFallSeconds, (move.From.Row - move.To.Row) * FallSecondsPerRow);
                falls.Join(view.rectTransform.DOAnchorPos(Position(move.To), seconds).SetEase(Ease.InQuad));
            }

            foreach ((Cell cell, Image view) in landed)
            {
                _grid[cell.Row, cell.Column] = view;
            }

            await Run(falls);
        }

        private async Awaitable ShuffleAsync(BoardResolution resolution, Board board)
        {
            if (resolution.Regenerated)
            {
                Sequence fade = DOTween.Sequence();
                foreach (Image view in _grid)
                {
                    if (view != null)
                    {
                        fade.Join(view.rectTransform.DOScale(0f, ShuffleSeconds * 0.5f).SetEase(Ease.InBack));
                    }
                }

                await Run(fade);
                Render(board);
                Sequence grow = DOTween.Sequence();
                foreach (Image view in _grid)
                {
                    if (view != null)
                    {
                        view.rectTransform.localScale = Vector3.zero;
                        grow.Join(view.rectTransform.DOScale(1f, ShuffleSeconds * 0.5f).SetEase(Ease.OutBack));
                    }
                }

                await Run(grow);
                return;
            }

            var moved = new Image[_rows, _columns];
            Sequence shuffle = DOTween.Sequence();
            foreach (GemMove move in resolution.Shuffle)
            {
                Image view = Grid(move.From);
                if (view == null)
                {
                    continue;
                }

                moved[move.To.Row, move.To.Column] = view;
                shuffle.Join(view.rectTransform.DOAnchorPos(Position(move.To), ShuffleSeconds).SetEase(Ease.InOutCubic));
            }

            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    if (moved[row, column] != null)
                    {
                        _grid[row, column] = moved[row, column];
                    }
                }
            }

            await Run(shuffle);
        }

        private Awaitable Run(Sequence sequence)
        {
            return sequence.SetTarget(this).SetLink(gameObject).Gate(_animations).AsAwaitable();
        }

        private void Select(Cell cell)
        {
            _hasSelection = true;
            _selected = cell;
            _selection.gameObject.SetActive(true);
            _selection.anchoredPosition = Position(cell);
        }

        private void ClearSelection()
        {
            _hasSelection = false;
            _dragging = false;
            _selection.gameObject.SetActive(false);
        }

        private bool TryCellAt(PointerEventData eventData, out Cell cell)
        {
            cell = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position, eventData.pressEventCamera,
                    out Vector2 point))
            {
                return false;
            }

            int column = Mathf.FloorToInt((point.x + _columns * _cellSize * 0.5f) / _cellSize);
            int row = Mathf.FloorToInt((point.y + _rows * _cellSize * 0.5f) / _cellSize);
            if (row < 0 || row >= _rows || column < 0 || column >= _columns)
            {
                return false;
            }

            cell = new Cell(row, column);
            return true;
        }

        private Vector2 Position(Cell cell)
        {
            return new Vector2((cell.Column + 0.5f - _columns * 0.5f) * _cellSize, (cell.Row + 0.5f - _rows * 0.5f) * _cellSize);
        }

        private Image Grid(Cell cell)
        {
            return cell.Row >= 0 && cell.Row < _rows && cell.Column >= 0 && cell.Column < _columns ? _grid[cell.Row, cell.Column] : null;
        }

        private bool InGrid(Image view)
        {
            foreach (Image item in _grid)
            {
                if (item == view)
                {
                    return true;
                }
            }

            return false;
        }

        private void Place(Image view, Cell cell, int kind)
        {
            DOTween.Kill(view.rectTransform);
            DOTween.Kill(view);
            view.sprite = kind >= 0 && kind < _sprites.Length ? _sprites[kind] : null;
            RectTransform rect = view.rectTransform;
            rect.sizeDelta = new Vector2(_cellSize * 0.86f, _cellSize * 0.86f);
            rect.anchoredPosition = Position(cell);
            rect.localScale = Vector3.one;
            Color color = view.color;
            color.a = 1f;
            view.color = color;
        }

        private Image Take()
        {
            foreach (Image view in _pool)
            {
                if (!view.gameObject.activeSelf)
                {
                    view.gameObject.SetActive(true);
                    return view;
                }
            }

            Image created = Instantiate(_gemTemplate, _area);
            created.gameObject.SetActive(true);
            _pool.Add(created);
            _selection.SetAsLastSibling();
            return created;
        }

        private static void Release(Image view)
        {
            if (view != null)
            {
                DOTween.Kill(view.rectTransform);
                DOTween.Kill(view);
                view.gameObject.SetActive(false);
            }
        }
    }
}
