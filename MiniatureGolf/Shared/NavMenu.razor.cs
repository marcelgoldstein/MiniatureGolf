using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace MiniatureGolf.Shared
{
    public class NavMenuModel : ComponentBase
    {

        protected bool collapseNavMenu = true;

        protected string NavMenuCssClass => collapseNavMenu ? "collapse" : null;

        protected void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        [Inject] protected IUriHelper uriHelper { get; set; }

        protected string CurrentPageName { get; set; }

        protected List<(string href, string name)> Pages { get; private set; } = new List<(string href, string name)>() { ("scoreboard", "Scoreboard" ), ( "scores", "Scores" ) };

        protected override void OnInit()
        {
            base.OnInit();

            this.uriHelper.OnLocationChanged += this.UriHelper_OnLocationChanged;
            this.RefreshCurrentPageLink();
        }

        private void UriHelper_OnLocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            this.RefreshCurrentPageLink();
        }

        private void RefreshCurrentPageLink()
        {
            var relativeUri = uriHelper.ToBaseRelativePath(uriHelper.GetBaseUri(), this.uriHelper.GetAbsoluteUri());
            var pageEntry = this.Pages.SingleOrDefault(a => relativeUri.ToUpper().StartsWith(a.href.ToUpper()));
            pageEntry = (pageEntry == default ? this.Pages[0] : pageEntry);
            this.CurrentPageName = pageEntry.name;

            this.StateHasChanged();
        }
    }
}
