namespace ChtotibDocsPrintNET.Models;

/// <summary>Параметры генерации PDF/предпросмотра.</summary>
public class PrintDocumentOptions
{
    /// <summary>Рисовать JPG-подложку (бланк). Если false — только текст на белом фоне.</summary>
    public bool DrawBackground { get; set; } = true;

    /// <summary>Надпись «Дубликат» на дипломе (лицо и оборот).</summary>
    public bool PrintDuplicate { get; set; }

    /// <summary>Заголовок «Квалификация» на обороте (не все шаблоны содержат его на подложке).</summary>
    public bool PrintQualificationLabel { get; set; }

    /// <summary>Шаблоны «с отличием» (DiplomaExcellent_*), если тип «С отличием» или все итоговые оценки = 5.</summary>
    public bool UseHonorTemplatesWhenApplicable { get; set; } = true;
}
