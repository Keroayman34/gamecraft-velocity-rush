using System;
using System.Collections.Generic;
using VelocityRush.Core;

namespace VelocityRush.Data
{
    /// <summary>
    /// Deterministic bilingual string table covering every UI label for EN and AR (D-013, D-069).
    /// All keys are populated at construction time; lookups are O(1) dictionary.
    /// </summary>
    public sealed class LocalizationTable : ILocalizationTable
    {
        private readonly Dictionary<string, string> _en = new Dictionary<string, string>(128, StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _ar = new Dictionary<string, string>(128, StringComparer.OrdinalIgnoreCase);

        public int KeyCount => _en.Count;

        public LocalizationTable()
        {
            PopulateTable();
        }

        public string Get(string key, string language)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var dict = string.Equals(language, LocalizationService.LanguageArabic, StringComparison.OrdinalIgnoreCase)
                ? _ar
                : _en;

            return dict.TryGetValue(key, out var value) ? value : null;
        }

        public bool HasKey(string key)
        {
            return !string.IsNullOrEmpty(key) && _en.ContainsKey(key) && _ar.ContainsKey(key);
        }

        /// <summary>Returns all registered keys (cold path, for test validation).</summary>
        public IReadOnlyCollection<string> AllKeys => _en.Keys;

        private void PopulateTable()
        {
            // ═══════════════════════════════════════════════════════
            // Main Menu
            // ═══════════════════════════════════════════════════════
            Add("menu.title", "Velocity Rush", "فيلوسيتي رش");
            Add("menu.play", "Play", "العب");
            Add("menu.garage", "Garage", "المرآب");
            Add("menu.settings", "Settings", "الإعدادات");
            Add("menu.credits_label", "Credits", "الرصيد");
            Add("menu.language_toggle", "عربي", "EN");

            // ═══════════════════════════════════════════════════════
            // Championship Selection
            // ═══════════════════════════════════════════════════════
            Add("champ.title", "Championships", "البطولات");
            Add("champ.locked", "Locked", "مقفل");
            Add("champ.unlocked", "Unlocked", "مفتوح");
            Add("champ.stars", "Stars", "النجوم");
            Add("champ.back", "Back", "رجوع");
            Add("champ.c1_name", "Ignition Series", "سلسلة الانطلاق");
            Add("champ.c2_name", "Apex Challenger", "تحدي القمة");
            Add("champ.c3_name", "Velocity Masters", "أسياد السرعة");
            Add("champ.c1_diff", "Beginner", "مبتدئ");
            Add("champ.c2_diff", "Intermediate", "متوسط");
            Add("champ.c3_diff", "Expert", "خبير");

            // ═══════════════════════════════════════════════════════
            // Event Selection
            // ═══════════════════════════════════════════════════════
            Add("event.title", "Events", "الأحداث");
            Add("event.round", "Round", "الجولة");
            Add("event.circuit", "Circuit Race", "سباق حلبة");
            Add("event.timetrial", "Time Trial", "تجربة زمنية");
            Add("event.best_time", "Best Time", "أفضل وقت");
            Add("event.no_time", "---", "---");
            Add("event.launch", "Launch", "انطلق");
            Add("event.locked", "Locked", "مقفل");
            Add("event.back", "Back", "رجوع");

            // ═══════════════════════════════════════════════════════
            // Garage & Upgrades
            // ═══════════════════════════════════════════════════════
            Add("garage.title", "Garage", "المرآب");
            Add("garage.select", "Select", "اختر");
            Add("garage.selected", "Selected", "مختار");
            Add("garage.buy", "Buy", "شراء");
            Add("garage.locked", "Locked", "مقفل");
            Add("garage.upgrades", "Upgrades", "الترقيات");
            Add("garage.back", "Back", "رجوع");
            Add("garage.class_starter", "Starter", "مبتدئ");
            Add("garage.class_sport", "Sport", "رياضية");
            Add("garage.class_drift", "Drift", "انزلاقية");
            Add("garage.class_supercar", "Supercar", "خارقة");
            Add("garage.insufficient_credits", "Insufficient Credits", "رصيد غير كافٍ");

            // Upgrade Categories
            Add("upgrade.engine", "Engine", "المحرك");
            Add("upgrade.tires", "Tires", "الإطارات");
            Add("upgrade.brakes", "Brakes", "المكابح");
            Add("upgrade.suspension", "Suspension", "التعليق");
            Add("upgrade.transmission", "Transmission", "ناقل الحركة");
            Add("upgrade.aerodynamics", "Aerodynamics", "الديناميكا الهوائية");
            Add("upgrade.tier", "Tier", "المستوى");
            Add("upgrade.stock", "Stock", "أساسي");
            Add("upgrade.max", "MAX", "الحد الأقصى");
            Add("upgrade.cost", "Cost", "التكلفة");
            Add("upgrade.buy", "Upgrade", "ترقية");
            Add("upgrade.back", "Back", "رجوع");

            // ═══════════════════════════════════════════════════════
            // Race HUD
            // ═══════════════════════════════════════════════════════
            Add("hud.speed_unit", "km/h", "كم/س");
            Add("hud.time_label", "Time", "الوقت");
            Add("hud.position_label", "Position", "الترتيب");
            Add("hud.lap_label", "Lap", "اللفة");
            Add("hud.countdown_go", "GO!", "!انطلق");

            // ═══════════════════════════════════════════════════════
            // Pause Overlay
            // ═══════════════════════════════════════════════════════
            Add("pause.title", "Paused", "إيقاف مؤقت");
            Add("pause.resume", "Resume", "استئناف");
            Add("pause.retry", "Retry", "إعادة المحاولة");
            Add("pause.settings", "Settings", "الإعدادات");
            Add("pause.quit", "Quit to Menu", "الخروج للقائمة");

            // ═══════════════════════════════════════════════════════
            // Results & Rewards
            // ═══════════════════════════════════════════════════════
            Add("results.title", "Race Complete", "انتهى السباق");
            Add("results.dnf", "Did Not Finish", "لم يكتمل");
            Add("results.position", "Position", "الترتيب");
            Add("results.time", "Time", "الوقت");
            Add("results.new_best", "New Best!", "رقم قياسي جديد!");
            Add("results.stars_earned", "Stars Earned", "النجوم المكتسبة");
            Add("results.base_reward", "Completion Bonus", "مكافأة الإكمال");
            Add("results.star_bonus", "Star Bonus", "مكافأة النجوم");
            Add("results.first_place_bonus", "1st Place Bonus", "مكافأة المركز الأول");
            Add("results.total_credits", "Total Credits", "إجمالي الرصيد");
            Add("results.next_event", "Next Event", "الحدث التالي");
            Add("results.retry", "Retry", "إعادة المحاولة");
            Add("results.menu", "Menu", "القائمة");
            Add("results.unlocked", "Unlocked!", "تم الفتح!");

            // ═══════════════════════════════════════════════════════
            // Settings Overlay
            // ═══════════════════════════════════════════════════════
            Add("settings.title", "Settings", "الإعدادات");
            Add("settings.master_volume", "Master Volume", "الصوت الرئيسي");
            Add("settings.sfx_volume", "SFX Volume", "المؤثرات الصوتية");
            Add("settings.engine_volume", "Engine Volume", "صوت المحرك");
            Add("settings.language", "Language", "اللغة");
            Add("settings.controls", "Controls", "التحكم");
            Add("settings.controls_ref", "Arrow Keys / WASD to drive\nSpace to brake\nEsc to pause", "مفاتيح الأسهم / WASD للقيادة\nمسافة للفرملة\nEsc للإيقاف");
            Add("settings.back", "Back", "رجوع");
            Add("settings.apply", "Apply", "تطبيق");

            // ═══════════════════════════════════════════════════════
            // Common / Shared
            // ═══════════════════════════════════════════════════════
            Add("common.cr", "CR", "ر.م");
            Add("common.loading", "Loading...", "جارٍ التحميل...");
            Add("common.error", "Error", "خطأ");
            Add("common.ok", "OK", "حسناً");
            Add("common.cancel", "Cancel", "إلغاء");
            Add("common.confirm", "Confirm", "تأكيد");

            // ═══════════════════════════════════════════════════════
            // Vehicle Names (OQ-14 approved)
            // ═══════════════════════════════════════════════════════
            Add("vehicle.starter-apex-v", "Apex V", "أبيكس V");
            Add("vehicle.starter-nova-hatch", "Nova Hatch", "نوفا هاتش");
            Add("vehicle.sport-velocity-gt", "Velocity GT", "فيلوسيتي GT");
            Add("vehicle.sport-aero-coupe", "Aero Coupe", "إيرو كوبيه");
            Add("vehicle.sport-pulse-r", "Pulse R", "بالس R");
            Add("vehicle.drift-sidewinder", "Sidewinder", "سايدوايندر");
            Add("vehicle.drift-kuma-dx", "Kuma DX", "كوما DX");
            Add("vehicle.supercar-vortex-v10", "Vortex V10", "فورتكس V10");
            Add("vehicle.supercar-eclipse", "Eclipse", "إكليبس");
            Add("vehicle.supercar-nebula-x", "Nebula X", "نيبولا X");
        }

        private void Add(string key, string en, string ar)
        {
            _en[key] = en;
            _ar[key] = ar;
        }
    }
}
