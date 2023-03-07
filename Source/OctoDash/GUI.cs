using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace GUI
{
    public abstract class GUIElement
    {
        protected Vector2 _position;
        protected Rectangle _rectangle;
        public Rectangle Rectangle { get { return _rectangle; } }

        protected GUIElement _next;
        protected GUIElement _up;
        protected GUIElement _down;
        protected GUIElement _left;
        protected GUIElement _right;

        virtual public GUIElement Next
        {
            get { return _next; }
            set { _next = value; }
        }
        virtual public GUIElement Up
        {
            get { return _up; }
            set { _up = value; }
        }
        virtual public GUIElement Down
        {
            get { return _down; }
            set { _down = value; }
        }
        virtual public GUIElement Left
        {
            get { return _left; }
            set { _left = value; }
        }
        virtual public GUIElement Right
        {
            get { return _right; }
            set { _right = value; }
        }

        protected static GUIElement _selected = null;
        public static GUIElement Selected
        {
            get { return _selected; }
        }
        public static void Unselect()
        {
            if (_selected != null)
            {
                _selected._color = _selected._defaultColor;
            }
            _selected = null;
        }

        protected bool _isSelectable = true;
        public bool IsSelectable { get { return _isSelectable; } }

        public int Width
        {
            get { return _rectangle.Width; }
        }
        public int Height
        {
            get { return _rectangle.Height; }
        }
        protected Color _color = Color.Cyan;
        protected Color _defaultColor = Color.Cyan;

        public Color SetColor
        {
            get { return _color; }
            set { _color = value; }
        }
        public Color Color
        {
            get { return _defaultColor; }
            set { _defaultColor = value; }
        }

        protected string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public delegate void ElementSelected(string element);

        protected event ElementSelected _selectEvent;
        virtual public event ElementSelected SelectEvent
        {
            add { _selectEvent += value; }
            remove { _selectEvent -= value; }
        }

        public void TriggerSelectEvent()
        {
            _selectEvent(_name);
        }
        public void TriggerSelect()
        {
            _selected = this;
            _color = Color.White;
            Log.Logger.Log("Selected " + _selected._name);
            TriggerSelectEvent();
        }

        public delegate void ElementClicked(string element);

        private event ElementClicked _clickEvent;
        virtual public event ElementClicked ClickEvent
        {
            add { _clickEvent += value; }
            remove { _clickEvent -= value; }
        }

        public void TriggerClickEvent()
        {
            Log.Logger.Log("Clicked on " + _name);
            _clickEvent(_name);
        }

        public GUIElement(string name)
        {
            _name = name;
            _position = new Vector2();
            _rectangle = new Rectangle();
        }

        abstract public void LoadContent(ContentManager content);

        virtual public void Update()
        {
            if (OctoDash.OctoDash.supportsMouse)
            {
                Point mousePos = new Point(Controls._mouseState.Position.X, Controls._mouseState.Position.Y);
                if (_isSelectable && Controls._mouseState.Position != Controls._oldMouseState.Position && _rectangle.Contains(mousePos))
                {
                    if (_selected == this) return;
                    Select();
                }
            }
        }
        virtual public GUIElement setNonSelectable()
        {
            _isSelectable = false;
            return this;
        }

        virtual public void Select()
        {
            if (!IsSelectable || this is ListElement || _selected == this) return;
            Unselect();
            TriggerSelect();
        }


        abstract public void Draw(SpriteBatch spriteBatch);

        virtual public GUIElement CenterElement(int width, int height)
        {
            _rectangle.X = (width - _rectangle.Width) / 2;
            _rectangle.Y = (height - _rectangle.Height) / 2;
            _position.X = _rectangle.X;
            _position.Y = _rectangle.Y;
            return this;
        }

        virtual public GUIElement CenterElement_leftAligned(int width, int height)
        {
            _rectangle.X = width / 2;
            _rectangle.Y = height / 2;
            _position.X = _rectangle.X;
            _position.Y = _rectangle.Y;
            return this;
        }


        virtual public GUIElement MoveElement(int x, int y)
        {
            _rectangle.X += x;
            _rectangle.Y += y;
            _position.X = _rectangle.X;
            _position.Y = _rectangle.Y;
            return this;
        }

        virtual public GUIElement MoveElement(Point p)
        {
            _rectangle.X += p.X;
            _rectangle.Y += p.Y;
            _position.X = _rectangle.X;
            _position.Y = _rectangle.Y;
            return this;
        }

        virtual public GUIElement MoveElementOntoPoint(Point p)
        {
            _rectangle.X = p.X;
            _rectangle.Y = p.Y;
            _position.X = _rectangle.X;
            _position.Y = _rectangle.Y;
            return this;
        }
    }

    public class ActionElement : GUIElement
    {

        public ActionElement(string name) : base(name) { }

        public override void LoadContent(ContentManager content) { }

        public override void Draw(SpriteBatch spriteBatch) { }

        public override void Select()
        {
            Log.Logger.Log("Selected " + _name);
            TriggerSelectEvent();
        }
    }

    public class GUIButton : GUIElement
    {
        private Texture2D _texture;

        public GUIButton(string name) : base(name) { }

        public override void LoadContent(ContentManager content)
        {
            _texture = content.Load<Texture2D>(_name);
            _rectangle = new Rectangle(0, 0, _texture.Width, _texture.Height);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, _rectangle, _color);
        }
    }


    public class TextElement : GUI.GUIElement
    {

        protected SpriteFont _font;
        protected string _fontName;
        private static string _defaultFont = "menu/sunnyspells25";

        private MutableTextElement _indicator;
        public MutableTextElement Indicator
        {
            get { return _indicator; }
            set { _indicator = value; }
        }

        public static string DefaultFont
        {
            get { return _defaultFont; }
            set { _defaultFont = value; }
        }
        protected string _displayedText;
        public string TextThatIsDisplayed
        {
            get { return (String.IsNullOrWhiteSpace(_displayedText)) ? _name : _displayedText; }
            set { _displayedText = value; }
        }

        public TextElement(string fontName, string name) : base(name)
        {
            _fontName = fontName;
            if (_fontName == "")
            {
                _fontName = _defaultFont;
            }
        }

        public override void LoadContent(ContentManager content)
        {
            _font = content.Load<SpriteFont>(_fontName);

            Vector2 textSize = _font.MeasureString(TextThatIsDisplayed);
            _rectangle = new Rectangle(0, 0,
                 (int)MathF.Ceiling(textSize.X),
                 (int)MathF.Ceiling(textSize.Y));
        }

        public override void Update()
        {
            if (_selected == this || _selected == Indicator) return;
            base.Update();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(_font, TextThatIsDisplayed, _position, _color);
        }

        public override void Select()
        {
            if (Indicator != null)
            {
                Indicator.Select();
            }
            else
            {
                Unselect();
                TriggerSelect();
            }
        }

    }

    public class MutableTextElement : TextElement
    {
        /* Note: other than it's parent, instances of this class are able to draw empty strings */

        private bool _cleared;
        public bool Cleared
        {
            get { return _cleared; }
            set { _cleared = value; }
        }

        public string Text
        {
            get { return _displayedText; }
            set { _displayedText = value; }
        }

        public MutableTextElement(string fontName, string name, string text) : base(fontName, name)
        {
            _displayedText = text;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(_font, _displayedText, _position, _color);
        }

    }

    public class ListElement : GUIElement
    {

        private List<GUIElement> _list;

        public List<GUIElement> List
        {
            get { return _list; }
        }

        public bool IsHorizontal;
        private int _spacing;
        public int Spacing
        {
            get { return _spacing; }
            set { _spacing = value; }
        }

        public override event ElementSelected SelectEvent
        {
            add
            {
                foreach (var item in List)
                {
                    item.SelectEvent += value;
                }
            }
            remove
            {
                foreach (var item in List)
                {
                    item.SelectEvent -= value;
                }
            }
        }

        public override event ElementClicked ClickEvent
        {
            add
            {
                foreach (var item in List)
                {
                    item.ClickEvent += value;
                }
            }
            remove
            {
                foreach (var item in List)
                {
                    item.ClickEvent -= value;
                }
            }
        }

        public ListElement(string name, bool isHorizontal, int spacing) : base(name)
        {
            _list = new List<GUIElement>();
            IsHorizontal = isHorizontal;
            Spacing = spacing;
        }

        public void Add(GUIElement element)
        {
            _list.Add(element);
        }

        public override void LoadContent(ContentManager content)
        {
            foreach (var elem in _list)
            {
                elem.LoadContent(content);
            }
            connectElements();
        }

        public override void Update()
        {
            foreach (var elem in _list)
            {
                elem.Update();
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            foreach (var elem in _list)
            {
                elem.Draw(spriteBatch);
            }
        }

        public ListElement connectElements()
        {

            GUIElement previous = this;
            GUIElement current = null;
            foreach (var item in _list)
            {
                previous.Next = item;
                previous = item;
            }
            previous.Next = this;
            previous = this;
            if (IsHorizontal)
            {
                foreach (var item in _list)
                {
                    if (!item.IsSelectable) continue;
                    current = item;
                    if (typeof(TextWithIndicator) == item.GetType())
                    {
                        current = ((TextWithIndicator)item).Indicator;
                    }
                    previous.Right = current;
                    current.Left = previous;
                    previous = current;
                }
                previous.Right = this;
                this.Left = previous;

                GUIElement l = this.Left;
                GUIElement r = this.Right;
                if (this.Left != null) this.Left.Right = r;
                if (this.Right != null) this.Right.Left = l;
            }
            else
            {
                foreach (var item in _list)
                {
                    if (!item.IsSelectable) continue;
                    current = item;
                    if (typeof(TextWithIndicator) == item.GetType())
                    {
                        current = ((TextWithIndicator)item).Indicator;
                    }
                    previous.Down = current;
                    current.Up = previous;
                    previous = current;
                }
                previous.Down = this;
                this.Up = previous;

                GUIElement u = this.Up;
                GUIElement d = this.Down;
                if (this.Up != null) this.Up.Down = d;
                if (this.Down != null) this.Down.Up = u;
            }

            return this;
        }

        virtual protected void adjustElementPositions()
        {
            Point p = _position.ToPoint();
            if (IsHorizontal)
            {
                foreach (var item in _list)
                {
                    item.MoveElementOntoPoint(p);
                    p.X += Spacing;
                }
            }
            else
            {
                foreach (var item in _list)
                {
                    item.MoveElementOntoPoint(p);
                    p.Y += Spacing;
                }
            }
        }

        public override void Select()
        {
            foreach (var item in List)
            {
                if (item.IsSelectable)
                {
                    item.Select();
                    break;
                }
            }
        }

        public override GUIElement setNonSelectable()
        {
            foreach (var item in _list)
            {
                item.setNonSelectable();
            }
            return base.setNonSelectable();
        }

        public override GUIElement CenterElement(int width, int height)
        {
            base.CenterElement(width, height);
            adjustElementPositions();
            return this;
        }

        public override GUIElement CenterElement_leftAligned(int width, int height)
        {
            base.CenterElement_leftAligned(width, height);
            adjustElementPositions();
            return this;
        }


        public override GUIElement MoveElement(int x, int y)
        {
            base.MoveElement(x, y);
            adjustElementPositions();
            return this;
        }

        public override GUIElement MoveElement(Point p)
        {
            base.MoveElement(p);
            adjustElementPositions();
            return this;
        }

        public override GUIElement MoveElementOntoPoint(Point p)
        {
            base.MoveElementOntoPoint(p);
            adjustElementPositions();
            return this;
        }

        public GUIElement FindElement(string element)
        {
            return _list.Find(x => x.Name.Equals(element));
        }

        public MutableTextElement FindIndicator(string element, string indicator)
        {
            return (MutableTextElement)((TextWithIndicator)_list.Find(x => x.Name.Equals(element))).List.Find(x => x.Name.Equals(indicator));
        }

        public MutableTextElement FindIndicator(string element)
        {
            return (MutableTextElement)((TextWithIndicator)_list.Find(x => x.Name.Equals(element))).Indicator;
        }
    }

    public class TextWithIndicator : ListElement
    {

        private float _indicatorOffset;
        private bool _drawOnlyIndicator = false;
        private TextElement _textElement;
        public TextElement TextElem
        {
            get { return _textElement; }
            set
            {
                _textElement = (TextElement)value;
                _textElement.Indicator = Indicator;
            }
        }

        private MutableTextElement _indicator;
        public MutableTextElement Indicator
        {
            get { return _indicator; }
            set { _indicator = value; }
        }

        public TextWithIndicator(string fontName, string indicator, int spacing) : base(indicator, true, spacing)
        {
            _indicator = new MutableTextElement(fontName, indicator, indicator);
            TextElem = new TextElement(fontName, indicator);
            _drawOnlyIndicator = true;

            List.Add(TextElem);
            List.Add(_indicator);
        }

        public TextWithIndicator(string fontName, string textElement, string indicator, int spacing, float indicatorOffset) : base(textElement, true, spacing)
        {
            _indicatorOffset = indicatorOffset;

            Indicator = new MutableTextElement(fontName, indicator, indicator);
            TextElem = new TextElement(fontName, textElement);

            List.Add(TextElem);
            List.Add(_indicator);
        }

        public TextWithIndicator(string fontName, string textElement, string indicator, int spacing) : base(textElement, true, spacing)
        {
            _indicator = new MutableTextElement(fontName, indicator, indicator);
            TextElem = new TextElement(fontName, textElement);

            List.Add(TextElem);
            List.Add(_indicator);
        }
        public TextWithIndicator(string fontName, string textElement, string indicator, bool isHorizontal, int spacing) : base(textElement, isHorizontal, spacing)
        {
            _indicator = new MutableTextElement(fontName, indicator, indicator);
            TextElem = new TextElement(fontName, textElement);

            List.Add(TextElem);
            List.Add(_indicator);
        }

        protected override void adjustElementPositions()
        {
            Point p = _position.ToPoint();

            int offset = (int)((_indicatorOffset == 0f) ? 5 * Spacing : _indicatorOffset);

            TextElem?.MoveElementOntoPoint(p);
            if (IsHorizontal)
            {
                p.X += Math.Max(offset, ((TextElem == null) ? 0 : TextElem.Width) + Spacing);
            }
            else
            {
                p.Y += Math.Max(offset, ((TextElem == null) ? 0 : TextElem.Height) + Spacing);
            }
            _indicator?.MoveElementOntoPoint(p);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_drawOnlyIndicator)
            {
                _indicator.Draw(spriteBatch);
            }
            else
            {
                base.Draw(spriteBatch);
            }
        }

        public override GUIElement Next
        {
            get { return _indicator.Next; }
            set { _indicator.Next = value; }
        }
        public override GUIElement Up
        {
            get { return _indicator.Up; }
            set { _indicator.Up = value; }
        }
        public override GUIElement Down
        {
            get { return _indicator.Down; }
            set { _indicator.Down = value; }
        }
        public override GUIElement Left
        {
            get { return _indicator.Left; }
            set { _indicator.Left = value; }
        }
        public override GUIElement Right
        {
            get { return _indicator.Right; }
            set { _indicator.Right = value; }
        }


        public override void Select()
        {
            Unselect();
            _indicator.Select();
        }

    }

}
