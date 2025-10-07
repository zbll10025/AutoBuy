using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YKF;
using Text = UnityEngine.UI.Text;
using UDebug = UnityEngine.Debug;
namespace AutoBuy
{
    public class ConfigUI:MonoBehaviour
    {
       
    }
    public class ConfigLayer : YKLayer<object>
    {
        public Button closeButton;
        public override Rect Bound { get; } = new Rect(0, 0, 320, 350);
        public override void OnLayout()
        {
            CustomTab tab =  CreateTab<CustomTab>("AuotoBuySetting", "setting1");
        }
        public static void CreateLayer(AutoBuy autoBuy)
        {
            YK.CreateLayer<ConfigLayer>();
        }


    }

    public class CustomTab : YKLayout<object>
    {
        public List<string> filterList = new List<string> { "Default", "Name", "Detail", "Tags", "Element" , "StockNum" };
        public const float textWidth = 110;
        public const float valueWidth = 150;
        public bool isShowIsAllMatch = false;
        public GameObject allMatchObject;
        public override void OnLayout()
        {
            YKVertical mainLayout = this.Vertical();
            keyWordItem(mainLayout);
            RerollNumItem(mainLayout);
            FilterMdoeItem(mainLayout);
            IsGuideItem(mainLayout);
            allMatchObject = IsAllMatchItem(mainLayout).gameObject;
            RefreshState(isShowIsAllMatch);
        }
        public YKHorizontal keyWordItem(YKVertical father)
        {
            YKHorizontal ho = father.Horizontal();
            var text = ho.Text("keyWord", color: FontColor.DontChange);
            SetSize(text, w: textWidth);
            string s = AutoBuy.keyword;
            UIInputSet(s, layOut: ho,onInput:(string value) => { AutoBuy.keyword = value;AutoBuy.Save(); });
            return ho;
        }
        public YKHorizontal RerollNumItem(YKVertical father)
        {
            YKHorizontal ho = father.Horizontal();
            UIText text =ho.Text("RerollNum", color: FontColor.DontChange);
            SetSize(text, w: textWidth);
            string s = AutoBuy.constRerollNum.ToString();
            var value = ho.InputText(s, onInput: (v) => { AutoBuy.constRerollNum = v; AutoBuy.Save(); });
            value.Find("Text").gameObject.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            SetSize(value, w: valueWidth);
            return ho;
        }
        public YKHorizontal FilterMdoeItem(YKVertical father)
        {
            YKHorizontal ho = father.Horizontal();
            UIText text = ho.Text("FilterMdoe", color: FontColor.DontChange);
            SetSize(text, w: textWidth);
            var value = ho.Dropdown(filterList, value: (int)AutoBuy.filterMdoe, action: (v) => {
                AutoBuy.filterMdoe = (AutoBuy.FilterMdoe)v;
                if (v == 1) {
                    isShowIsAllMatch = true;
                }
                else {
                    isShowIsAllMatch = false;
                }
                RefreshState(isShowIsAllMatch);
                AutoBuy.Save();
            });
            if ((int)AutoBuy.filterMdoe == 1)
            {
                isShowIsAllMatch = true;
            }
            else
            {
                isShowIsAllMatch = false;
            }
            SetSize(value, w: valueWidth);
            return ho;
        }
        public YKHorizontal IsGuideItem(YKVertical father)
        {
            YKHorizontal ho = father.Horizontal();
            UIText text = ho.Text("IsGuide", color: FontColor.DontChange);
            SetSize(text, w: textWidth);
            ho.Toggle("", isOn: AutoBuy.isGuide, onClick: (b) => { AutoBuy.isGuide = b; AutoBuy.Save(); });
            return ho;

        }
        public YKHorizontal IsAllMatchItem(YKVertical father)
        {
            YKHorizontal ho = father.Horizontal();
            UIText text =ho.Text("IsAllMatch", color: FontColor.DontChange);
            SetSize(text, w: textWidth);
            ho.Toggle("", isOn: AutoBuy.isAllMatch, onClick: (b) => { AutoBuy.isAllMatch = b; AutoBuy.Save(); });
            return ho;

        }
        public void RefreshState(bool b)
        {
            if(allMatchObject == null)
            {
                return;
            }
            allMatchObject.SetActive(b);
        }
        public void SetSize( Component com,float h = 36,float w =100)
        {
            com.Rect().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            com.GetOrCreate<LayoutElement>().preferredWidth = w;
        }
        public void UIInputSet(string text, Action<string> onInput = null,YKLayout layOut = null)
        {
            
            Widget widget = Util.Instantiate<Widget>("UI/Widget/WidgetSearch", this)
              ?? Util.Instantiate<Widget>("UI/Widget/WidgetSearch/WidgetSearch", this);
            widget.gameObject.SetActive(false);
            Transform component = widget.transform.Find("Search Box");
            Transform d1 = widget.transform.Find("Search Box/ButtonGeneral");
            Transform d2 = widget.transform.Find("Search Box/ButtonGeneral (1)");
            component.SetParent(layOut.transform);
            widget.transform.DestroyObject();
            d1.DestroyObject();
            d2.DestroyObject();

            InputField input = component.Find("InputField").GetComponent<InputField>();
            if (onInput != null)
            {
                input.onValueChanged.AddListener(value => onInput?.Invoke(value));
            }
            input.text = text;
            input.gameObject.GetComponent<CanvasRenderer>().SetColor(new Color(0.8f, 0.8f, 0.8f, 0.2f));
            input.Find("Text").gameObject.GetComponent<Text>().color = new Color(0.28f, 0.22f, 0.13f, 1f);
            component.Rect().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40f);
            component.GetOrCreate<LayoutElement>().preferredWidth = 150f;
            return;
        }
    }

}
