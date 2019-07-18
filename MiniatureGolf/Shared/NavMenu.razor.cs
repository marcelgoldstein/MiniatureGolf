using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MiniatureGolf.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        [Inject] protected IOptions<AppSettings> AppSettings { get; set; }

        protected string CurrentPageName { get; set; }
        public string SpecialDaysHeadertext { get; set; }

        protected List<(string href, string name)> Pages { get; private set; } = new List<(string href, string name)>() { ("scoreboard", "Scoreboard"), ("scores", "Scores") };

        protected override void OnInit()
        {
            base.OnInit();

            this.uriHelper.OnLocationChanged += this.UriHelper_OnLocationChanged;
            this.RefreshCurrentPageLink();

            this.RefreshSpecialDaysHeadertext();
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

        private void RefreshSpecialDaysHeadertext()
        {
            try
            {
                if (this.AppSettings.Value.SpecialDays is SpecialDays sd)
                {
                    if (string.IsNullOrWhiteSpace(sd.Headertext) || string.IsNullOrWhiteSpace(sd.Dates))
                    {
                        this.SpecialDaysHeadertext = null;
                        return;
                    }
                    else
                    {
                        var dates = new List<DateTime>();

                        var dateParts = sd.Dates.Split(',');
                        foreach (var datePart in dateParts)
                        {
                            var datePartSpanArray = datePart.Split('-');
                            var dateFrom = Convert.ToDateTime(datePartSpanArray[0], new CultureInfo("de-DE")).Date;
                            if (datePartSpanArray.Length == 1)
                            {
                                dates.Add(dateFrom);
                            }
                            else
                            {
                                var dateTo = Convert.ToDateTime(datePartSpanArray[1], new CultureInfo("de-DE")).Date;

                                var curDate = dateFrom;
                                while (curDate <= dateTo)
                                {
                                    dates.Add(curDate);
                                    curDate = curDate.AddDays(1);
                                }
                            }
                        }

                        if (dates.Contains(DateTime.Today.Date))
                        {
                            this.SpecialDaysHeadertext = (sd.Headertext + " ");
                        }
                    }
                }
            }
            catch (Exception)
            {
                this.SpecialDaysHeadertext = null;
            }
        }
    }
}
