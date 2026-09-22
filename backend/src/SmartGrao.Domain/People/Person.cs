using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Domain.People;

public readonly record struct PersonId(Guid Value) : IStronglyTypedId
{
    public static PersonId New() => new(Guid.CreateVersion7());
}

public enum PersonRole
{
    Undefined = 0,
    Agronomist,
    Technician,
    Operator,
    Producer,
    Other,
}

/// <summary>
/// Quem vai a campo. Cadastro simples, sem login: a vistoria precisa de um responsavel com
/// identidade estavel muito antes de o sistema ter autenticacao.
/// <para>
/// Guardar o nome digitado direto na vistoria seria mais rapido hoje e mais caro depois — "Joao",
/// "Joao S." e "joao" virariam tres pessoas, e o historico precisaria de reconciliacao manual
/// quando a autenticacao chegar. Com a pessoa como entidade, basta ela ganhar o vinculo com o
/// usuario, e o que ja foi registrado continua valendo.
/// </para>
/// <para>
/// Pertence a uma fazenda. Nao porque um tecnico nao possa atender varias, mas porque a fazenda e a
/// ancora de acesso de todo o resto do sistema, e sair dela agora criaria uma entidade solta
/// justamente no lugar por onde o isolamento vai entrar depois.
/// </para>
/// </summary>
public sealed class Person : AggregateRoot<PersonId>
{
    public const int MaximumNameLength = 120;

    private Person() { }

    public FarmId FarmId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public PersonRole Role { get; private set; }

    /// <summary>
    /// Quem saiu da equipe e desativado, nunca excluido: a vistoria que ele fez continua apontando
    /// para ele, e apagar a linha levaria junto o registro de quem esteve naquele talhao.
    /// </summary>
    public bool Active { get; private set; } = true;

    public static Person Create(FarmId farmId, string name, PersonRole role)
    {
        if (!Enum.IsDefined(role) || role == PersonRole.Undefined)
            throw new DomainException(SmartGraoErrors.Inspection.UnknownRole);

        return new Person
        {
            Id = PersonId.New(),
            FarmId = farmId,
            Name = ValidateName(name),
            Role = role,
        };
    }

    public void Update(string name, PersonRole role)
    {
        if (!Enum.IsDefined(role) || role == PersonRole.Undefined)
            throw new DomainException(SmartGraoErrors.Inspection.UnknownRole);

        Name = ValidateName(name);
        Role = role;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        if (!Active) return;
        Active = false;
        MarkAsUpdated();
    }

    public void Reactivate()
    {
        if (Active) return;
        Active = true;
        MarkAsUpdated();
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaximumNameLength)
            throw new DomainException(SmartGraoErrors.Inspection.InvalidPersonName);

        return name.Trim();
    }
}
