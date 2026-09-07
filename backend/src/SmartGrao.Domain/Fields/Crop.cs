namespace SmartGrao.Domain.Fields;

/// <summary>
/// Cultura plantada no talhao. Fechada num enum de proposito: no Pilar 2 o diagnostico depende dela
/// — o modelo de visao computacional que reconhece ferrugem asiatica so faz sentido sobre soja — e
/// um campo de texto livre tornaria esse roteamento impossivel.
/// </summary>
public enum Crop
{
    Undefined = 0,
    Soybean,
    Corn,
    Cotton,
    Coffee,
    Sugarcane,
    Wheat,
    Beans,
    Rice,
    Sorghum,
    Other,
}
