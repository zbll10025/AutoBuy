using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;
using YKF;
using static AutoBuy.AutoBuy;
using static BGMData;
using static HotItemLayout;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.UI.CanvasScaler;
using static UnityEngine.UI.Image;
using Text = UnityEngine.UI.Text;
using UDebug = UnityEngine.Debug;
namespace AutoBuy
{
    public class ConfigLayer : YKLayer<object>
    {
        public Button closeButton;
        public CustomTab settingTab;
        public BanCustomTab banSettingTab;
        public override Rect Bound { get; } = new Rect(0, 0, 700, 700);
        public override void OnLayout()
        {
             settingTab =  CreateTab<CustomTab>("AutoBuySetting", "setting1");
             banSettingTab = CreateTab<BanCustomTab>("BanSetting", "setting1");
        }
        public static void CreateLayer()
        {
            // 检查是否已经存在 ConfigLayer 实例
            ConfigLayer val = EMono.ui.layers.Find((Layer o) => o.GetType() == typeof(ConfigLayer)) as ConfigLayer;
            if (val != null)
            {
                val.gameObject.SetActive(true);
                return;
            }

            ConfigLayer layer = YK.CreateLayer<ConfigLayer>();
            AutoBuy.layer = layer;
            return ;
        }

        public static UIInputText InputItem(YKLayout layout, string text, Action<string> onInput = null)
        {
            var pair = layout.Horizontal();
            pair.Layout.childForceExpandWidth = true;
            var input = pair.InputText(text);
            input.type = UIInputText.Type.Name;
            input.field.characterLimit = 150;
            input.field.contentType = InputField.ContentType.Standard;
            input.field.inputType = InputField.InputType.Standard;
            input.field.characterValidation = InputField.CharacterValidation.None;
            if (onInput != null)
            {
                input.field.onValueChanged.AddListener(value => onInput?.Invoke(value));
            }
            input.Text = text;

            return input;
        }
    }
}
