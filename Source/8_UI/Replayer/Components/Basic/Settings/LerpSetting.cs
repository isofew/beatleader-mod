using BeatSaberMarkupLanguage.Attributes;
using UnityEngine.UI;
using UnityEngine;
using BeatLeader.Models;

namespace BeatLeader.Components {
    internal class LerpSetting : ReeUIComponentV2 {
        #region Configuration

        private const float MinAvailableSpeedMultiplier = 0.0f;
        private const float MaxAvailableSpeedMultiplier = 1.0f;
        private const float StartMult = 0.5f;

        #endregion

        #region UI Components

        [UIComponent("slider-container")]
        private readonly RectTransform _sliderContainer = null!;

        [UIComponent("handle")]
        private readonly Image _handle = null!;

        private Slider _slider = null!;

        #endregion

        #region Setup

        private IVirtualPlayersManager _playersMan = null!;
        private bool _isInitialized;

        public void Setup(IVirtualPlayersManager playersManager) {
            _playersMan = playersManager;
            _isInitialized = true;
            OnSliderDrag(StartMult * 10);
        }

        protected override void OnInitialize() {
            _slider = _sliderContainer.gameObject.AddComponent<Slider>();
            _slider.targetGraphic = _handle;
            _slider.handleRect = _handle.rectTransform;
            _slider.minValue = MinAvailableSpeedMultiplier * 10;
            _slider.maxValue = MaxAvailableSpeedMultiplier * 10;
            _slider.wholeNumbers = true;
            _slider.onValueChanged.AddListener(OnSliderDrag);
        }

        #endregion

        #region SpeedMultiplierText

        [UIValue("lerp-multiplier-text")]
        public string SpeedMultiplierText {
            get => _speedMultiplierText;
            private set {
                _speedMultiplierText = value;
                NotifyPropertyChanged(nameof(SpeedMultiplierText));
            }
        }

        private string _speedMultiplierText = null!;

        #endregion

        #region UI Callbacks

        private void OnSliderDrag(float value) {
            if (!_isInitialized 
                || float.IsInfinity(value) 
                || float.IsNaN(value)) return;

            var mult = value * 0.1f;
            mult = Mathf.Clamp(mult, 
                MinAvailableSpeedMultiplier, MaxAvailableSpeedMultiplier);

            _playersMan.PriorityPlayer!.lerpMultiplier = mult;

            RefreshText(mult);
        }

        #endregion

        #region RefreshText

        private void RefreshText(float mul) {
            string currentMulColor =
                mul.Equals(StartMult) ? "yellow" :
                mul.Equals(MinAvailableSpeedMultiplier) ||
                mul.Equals(MaxAvailableSpeedMultiplier) ? "red" : "#00ffffff" /*cyan*/;

            SpeedMultiplierText = $"<color={currentMulColor}>{mul * 100}%</color>" +
                $" | <color=yellow>{StartMult * 100}%</color>";
        }

        #endregion
    }
}
