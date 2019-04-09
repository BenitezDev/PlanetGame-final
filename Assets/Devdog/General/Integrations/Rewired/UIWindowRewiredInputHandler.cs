#if REWIRED

using UnityEngine;
using Devdog.General.UI;
using UnityEngine.Assertions;

namespace Devdog.General.Integration.RewiredIntegration
{
    [RequireComponent(typeof(UIWindow))]
    public class UIWindowRewiredInputHandler : MonoBehaviour, IUIWindowInputHandler
    {

        [SerializeField]
        private RewiredHelper _helper = new RewiredHelper();

        private UIWindow _window;

        protected void Awake()
        {
            _helper.Init();

            _window = GetComponent<UIWindow>();
            Assert.IsNotNull(_window, "No window found on object");
        }

        protected void Update()
        {
            if (_helper.player.GetButtonDown(_helper.rewiredActionName))
            {
                _window.Toggle();
            }
        }
    }
}

#endif