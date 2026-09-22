using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.People;

namespace SmartGrao.Application.Features.Inspections;

public sealed class GetPeopleQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<PersonViewModel>> HandleAsync(
        Guid farmId, bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var id = new FarmId(farmId);
        if (!await db.Farms.AnyAsync(x => x.Id == id, cancellationToken))
            throw new DomainException(SmartGraoErrors.Farm.NotFound);

        var query = db.People.AsNoTracking().Where(x => x.FarmId == id);
        if (activeOnly) query = query.Where(x => x.Active);

        var people = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return people.Select(PersonViewModel.From).ToList();
    }
}

public sealed class CreatePersonCommandHandler(
    ISmartGraoDbContext db, IValidator<CreatePersonViewModel> validator)
{
    public async Task<PersonViewModel> HandleAsync(
        CreatePersonViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var farmId = new FarmId(model.FarmId);
        if (!await db.Farms.AnyAsync(x => x.Id == farmId, cancellationToken))
            throw new DomainException(SmartGraoErrors.Farm.NotFound);

        var person = Person.Create(farmId, model.Name, model.Role);
        db.People.Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return PersonViewModel.From(person);
    }
}

public sealed class UpdatePersonCommandHandler(
    ISmartGraoDbContext db, IValidator<UpdatePersonViewModel> validator)
{
    public async Task<PersonViewModel> HandleAsync(
        Guid id, UpdatePersonViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var person = await Load(db, id, cancellationToken);
        person.Update(model.Name, model.Role);
        await db.SaveChangesAsync(cancellationToken);

        return PersonViewModel.From(person);
    }

    internal static async Task<Person> Load(ISmartGraoDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var personId = new PersonId(id);
        return await db.People.FirstOrDefaultAsync(x => x.Id == personId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Inspection.PersonNotFound);
    }
}

/// <summary>
/// Tira ou devolve alguem a equipe. Nao existe exclusao: a vistoria que a pessoa fez continua
/// apontando para ela, e apagar a linha levaria junto o registro de quem esteve naquele talhao.
/// </summary>
public sealed class SetPersonStatusCommandHandler(ISmartGraoDbContext db)
{
    public async Task<PersonViewModel> HandleAsync(
        Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var person = await UpdatePersonCommandHandler.Load(db, id, cancellationToken);

        if (active) person.Reactivate();
        else person.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
        return PersonViewModel.From(person);
    }
}
