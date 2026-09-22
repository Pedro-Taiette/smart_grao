using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.People;
using Xunit;

namespace SmartGrao.Domain.Tests.Inspections;

public sealed class PersonTests
{
    [Fact]
    public void ANewPersonIsOnTheTeamAndHasTheNameTrimmed()
    {
        var person = Person.Create(FarmId.New(), "  Ana Souza  ", PersonRole.Agronomist);

        Assert.Equal("Ana Souza", person.Name);
        Assert.Equal(PersonRole.Agronomist, person.Role);
        Assert.True(person.Active);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ANameIsRequired(string name)
    {
        var error = Assert.Throws<DomainException>(
            () => Person.Create(FarmId.New(), name, PersonRole.Technician));

        Assert.Equal("inspection.invalid_person_name", error.Error.Code);
    }

    [Fact]
    public void ANameLongerThanTheLimitIsRejected()
    {
        Assert.Throws<DomainException>(() => Person.Create(
            FarmId.New(), new string('x', Person.MaximumNameLength + 1), PersonRole.Technician));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void AnUndefinedOrUnknownRoleIsRejected(int role)
    {
        var error = Assert.Throws<DomainException>(
            () => Person.Create(FarmId.New(), "Ana", (PersonRole)role));

        Assert.Equal("inspection.unknown_role", error.Error.Code);
    }

    /// <summary>
    /// Quem saiu da equipe e desativado, nunca excluido: a vistoria que ele fez continua apontando
    /// para ele, e apagar a linha levaria junto o registro de quem esteve naquele talhao.
    /// </summary>
    [Fact]
    public void LeavingTheTeamIsReversibleAndIdempotent()
    {
        var person = Person.Create(FarmId.New(), "Ana", PersonRole.Technician);

        person.Deactivate();
        var touched = person.UpdatedAt;
        person.Deactivate();
        Assert.False(person.Active);
        Assert.Equal(touched, person.UpdatedAt);

        person.Reactivate();
        Assert.True(person.Active);
    }
}
