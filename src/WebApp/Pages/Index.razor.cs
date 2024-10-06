using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoGameApp = GameApp.Shared.GameApp;

namespace WebApp.Pages
{
    public partial class Index
    {
        private MonoGameApp? _game;
        private DotNetObjectReference<Index>? _objRef;
        private DotNetObjectReference<Index> ObjRef => _objRef ??= DotNetObjectReference.Create(this);

        protected override async Task OnInitializedAsync()
        {
            await JsRuntime.InvokeVoidAsync("interopHelper.storeObjectRef", ObjRef);
        }

        [JSInvokable]
        public void EventListener(IDictionary<string, object> input)
        {
            _game?.UpdateWebInput(input);
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void TickDotNet()
        {
            // init game
            if (_game == null)
            {
                _game = new MonoGameApp();
                _game.Run();
            }

            // run gameloop
            _game.Tick();
        }

    }
}
