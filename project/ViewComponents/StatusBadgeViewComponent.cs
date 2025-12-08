using Microsoft.AspNetCore.Mvc;

namespace proje.ViewComponents
{
    public class StatusBadgeViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(bool isActive, string? iconClass = null, string? badgeSize = null, bool showIcon = true)
        {
            ViewBag.IsActive = isActive;
            ViewBag.IconClass = iconClass ?? "bi-toggle-on";
            ViewBag.BadgeSize = badgeSize ?? "fs-6";
            ViewBag.ShowIcon = showIcon;
            return View();
        }
    }
}

