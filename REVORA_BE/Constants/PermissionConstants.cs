namespace REVORA_BE.Constants
{
    public static class PermissionConstants
    {
        public static class Users
        {
            public const string View = "user.profile.view";
            public const string Edit = "user.profile.edit";
            public const string Follow = "user.follow";
            public const string ManageAll = "user.manage.all";
        }

        public static class Products
        {
            public const string View = "product.view";
            public const string Create = "product.create";
            public const string UpdateOwn = "product.update.own";
            public const string DeleteOwn = "product.delete.own";
            public const string ManageAll = "product.manage.all";
        }

        public static class Comments
        {
            public const string Create = "comment.create";
            public const string DeleteOwn = "comment.delete.own";
            public const string Like = "comment.like";
            public const string ManageAll = "comment.manage.all";
        }

        public static class Shorts
        {
            public const string View = "short.view";
            public const string Upload = "short.upload";
            public const string DeleteOwn = "short.delete.own";
            public const string Like = "short.like";
            public const string ManageAll = "short.manage.all";
        }

        public static class Credits
        {
            public const string ViewOwn = "credit.view.own";
            public const string Purchase = "credit.purchase";
            public const string AwardManual = "credit.award.manual";
            public const string ManageConfig = "credit.manage.config";
        }

        public static class System
        {
            public const string CategoryManage = "system.category.manage";
            public const string BadgeManage = "system.badge.manage";
            public const string AuditView = "system.audit.view";
            public const string SettingsEdit = "system.settings.edit";
        }

        /// <summary>
        /// Retrieves all defined permissions.
        /// </summary>
        public static IEnumerable<string> GetAllPermissions()
        {
            return new[]
            {
                Users.View, Users.Edit, Users.Follow, Users.ManageAll,
                Products.View, Products.Create, Products.UpdateOwn, Products.DeleteOwn, Products.ManageAll,
                Comments.Create, Comments.DeleteOwn, Comments.Like, Comments.ManageAll,
                Shorts.View, Shorts.Upload, Shorts.DeleteOwn, Shorts.Like, Shorts.ManageAll,
                Credits.ViewOwn, Credits.Purchase, Credits.AwardManual, Credits.ManageConfig,
                System.CategoryManage, System.BadgeManage, System.AuditView, System.SettingsEdit
            };
        }

        /// <summary>
        /// Retrieves the basic operational permissions for the default 'User' role.
        /// </summary>
        public static IEnumerable<string> GetUserRolePermissions()
        {
            return new[]
            {
                Users.View, Users.Edit, Users.Follow,
                Products.View, Products.Create, Products.UpdateOwn, Products.DeleteOwn,
                Comments.Create, Comments.DeleteOwn, Comments.Like,
                Shorts.View, Shorts.Upload, Shorts.DeleteOwn, Shorts.Like,
                Credits.ViewOwn, Credits.Purchase
            };
        }
    }
}
