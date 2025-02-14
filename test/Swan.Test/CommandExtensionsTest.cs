﻿namespace Swan.Test;

using Mocks;
using System.Data;

[TestFixture]
public class CommandExtensionsTest
{
    [Test]
    public void PrepareNullCommandReturnsFalse()
    {
        SqliteCommand? command = null;
        var prepared = command.TryPrepare();

        Assert.IsFalse(prepared);
    }

    [Test]
    public void PrepareCommandWithConnectionClosedReturnsFalse()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        var prepared = command.TryPrepare();

        Assert.IsFalse(prepared);
    }

    [Test]
    public void PrepareCommandWithConnectionOpenReturnsTrue()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var command = conn.CreateCommand();
        var prepared = command.TryPrepare();

        Assert.IsTrue(prepared);
    }

    [Test]
    public void PrepareNullCommandThrowsExceptionAsync()
    {
        SqliteCommand? command = null;

        Assert.ThrowsAsync<ArgumentNullException>(() => command.TryPrepareAsync());
    }

    [Test]
    public async Task PrepareCommandWithConnectionClosedReturnsFalseAsync()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        var prepared = await command.TryPrepareAsync();

        Assert.IsFalse(prepared);
    }

    [Test]
    public async Task PrepareCommandWithConnectionOpenReturnsTrueAsync()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var command = conn.CreateCommand();
        var prepared = await command.TryPrepareAsync();

        Assert.IsTrue(prepared);
    }

    [Test]
    public void TryFindParameterInANullCommandReturnsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.TryFindParameter("Parameter", out var parameter));
    }

    [Test]
    public void TryFindParameterWhenCommandConnectionIsNullThrowsException()
    {
        var conn = new SqliteConnection();
        var command = conn.CreateCommand();
        command.Connection = null;

        Assert.Throws<ArgumentException>(() => command.TryFindParameter("Parameter", out var parameter));
    }

    [Test]
    public void TryFindParameterWhenNameIsNullOrWhiteSpaceThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        Assert.Throws<ArgumentNullException>(() => command.TryFindParameter("", out var parameter));
    }

    [Test]
    public void TryFindParameterWhenNameIsValidButDoesntExistsReturnsFalse()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        var found = command.TryFindParameter("Parameter", out var parameter);

        Assert.IsFalse(found);
    }

    [Test]
    public void TryFindParameterWhenNameIsValidAndExistsReturnsTrue()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        command.Parameters.Add("TestParameter", SqliteType.Text);

        var found = command.TryFindParameter("TestParameter", out var parameter);

        Assert.IsTrue(found);
    }

    [Test]
    public void GetNamedParametersWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.GetNamedParameters().Any());
    }

    [Test]
    public void GetNamedParametersReturnsTrueIfAnyExists()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        command.Parameters.Add("TestParameter", SqliteType.Text);

        var found = command.GetNamedParameters().Any();

        Assert.IsTrue(found);
    }

    [Test]
    public void DefineParameterWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.DefineParameter("IntParameter", DbType.Int32));
    }

    [Test]
    public void DefineParameterWhenNameIsNullOrWhiteSpaceThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        Assert.Throws<ArgumentNullException>(() => command.DefineParameter("", DbType.Int32));
    }

    [Test]
    public void DefineParameterCreatedOk()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        var created = command.DefineParameter("IntParameter", DbType.Int32);

        Assert.IsNotNull(created);
    }

    [Test]
    public void DefineParameterWithClrTypeWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.DefineParameter("IntParameter", typeof(int)));
    }

    [Test]
    public void DefineParameterClrTypeWhenConnectionIsNullThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        command.Connection = null;

        Assert.Throws<ArgumentException>(() => command.DefineParameter("IntParameter", typeof(int)));
    }

    [Test]
    public void DefineParameterClrTypeWhenNameIsNullOrWhiteSpaceThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        Assert.Throws<ArgumentNullException>(() => command.DefineParameter("", typeof(int)));
    }

    [Test]
    public void DefineParameterWhenClrTypeIsNullThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        Assert.Throws<ArgumentNullException>(() => command.DefineParameter("IntParameter", null));
    }

    [Test]
    public void DefineParametersWhenCommandIsNullThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var table = conn.EnsureConnected().TableBuilder<Project>("Projects").ExecuteTableCommand();
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.DefineParameters(table.Columns));
    }

    [Test]
    public void DefineParametersWorksOk()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var table = conn.EnsureConnected().TableBuilder<Project>("Projects").ExecuteTableCommand();
        var command = conn.CreateCommand();
        var parameters = command.DefineParameters(table.Columns);

        Assert.IsNotNull(parameters);
    }

    [Test]
    public void SetParameterWithNoSpecificValue()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        var set = command.SetParameter("IntParameter", typeof(int));

        Assert.IsNotNull(set.Parameters);
    }

    [Test]
    public void SetParameterWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.SetParameter("IntParameter", 0, typeof(int), 1));
    }

    [Test]
    public void SetParameterWhenNameIsNullOrWhiteSpaceThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        Assert.Throws<ArgumentNullException>(() => command.SetParameter("", 0, typeof(int), 1));
    }

    [Test]
    public void SetParameterWhenAlreadyExistsUpdateValue()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();


        command.SetParameter("IntParameter", 0, typeof(int), 1);
        var firstValue = command.Parameters[0].Value;

        command.SetParameter("IntParameter", 1, typeof(int), 1);
        var secondValue = command.Parameters[0].Value;

        Assert.AreNotEqual(firstValue, secondValue);
    }

    [Test]
    public void SetParameterWithDbTypeWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.SetParameter("IntParameter", 0, DbType.Int32, 1));
    }

    [Test]
    public void SetParameterWithDbTypeWhenNameIsNullOrWhiteSpaceThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        Assert.Throws<ArgumentNullException>(() => command.SetParameter("", 0, DbType.Int32, 1));
    }

    [Test]
    public void SetParameterWithDbTypeWhenAlreadyExistsUpdateValue()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();


        command.SetParameter("IntParameter", 0, DbType.Int32, 1);
        var firstValue = command.Parameters[0].Value;

        command.SetParameter("IntParameter", 1, DbType.Int32, 1);
        var secondValue = command.Parameters[0].Value;

        Assert.AreNotEqual(firstValue, secondValue);
    }

    [Test]
    public void SetParametersWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;
        var project = new Project();

        Assert.Throws<ArgumentNullException>(() => command.SetParameters(project));
    }

    [Test]
    public void SetParametersWhenCommandConnectionIsNullThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        command.Connection = null;
        var project = new Project();

        Assert.Throws<ArgumentException>(() => command.SetParameters(project));
    }

    [Test]
    public void SetParametersRunsOk()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        var cmd = command.SetParameters(new Project()
        {
            CompanyId = 1,
            EndDate = DateTime.Now,
            IsActive = true,
            Name = "Project ONE",
            ProjectScope = "My Scope",
            ProjectType = ProjectTypes.Exciting,
            StartDate = DateTime.Now.AddMonths(-1)
        });

        Assert.AreEqual(8, cmd.Parameters.Count);
    }

    [Test]
    public void WithProperties()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.WithProperties());
    }

    [Test]
    public void WithPropertiesWhenCommandIsNullThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var tran = conn.BeginTransaction();
        var timeSpan = new TimeSpan(0, 0, 30);
        var command = conn.CreateCommand();

        var cmdWithProperties = command.WithProperties("Select 1;", CommandType.Text, tran, timeSpan);

        Assert.AreEqual("Select 1;", cmdWithProperties.CommandText);
        Assert.AreEqual(timeSpan.Seconds, cmdWithProperties.CommandTimeout);
        Assert.AreEqual(CommandType.Text, cmdWithProperties.CommandType);
    }

    [Test]
    public void AppendTextsWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.AppendText("Where 1 = 1"));
    }

    [Test]
    public void AppendTextsWhenCommandTextIsEmptyThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var command = conn.CreateCommand();

        var commandPlusText = command.AppendText("Select 1");

        Assert.AreEqual("Select 1", commandPlusText.CommandText);
    }

    [Test]
    public void AppendTextsWhenCommandTextIsNotEmptyThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var command = conn.CreateCommand();
        command.CommandText = "Select 1";

        var commandPlusText = command.AppendText("Where 1 = 1;");

        Assert.AreEqual("Select 1 Where 1 = 1;", commandPlusText.CommandText);
    }

    [Test]
    public void WithTextsWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.WithText("Select 1"));
    }

    [Test]
    public void WithTextCommandTextIsNullSetsCommandTextToEmpty()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var command = conn.CreateCommand();

        command.WithText(null);

        Assert.AreEqual(string.Empty, command.CommandText);
    }

    [Test]
    public void WithTextSetsCommandText()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var command = conn.CreateCommand();

        command.WithText("Select 1");

        Assert.AreEqual("Select 1", command.CommandText);
    }

    [Test]
    public void WithTransactionWhenCommandIsNullThrowsException()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var tran = conn.BeginTransaction();

        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.WithTransaction(tran));
    }

    [Test]
    public void WithTransactionSetsCommandTransaction()
    {
        var conn = new SqliteConnection("Data Source=:memory:").EnsureConnected();
        var command = conn.CreateCommand();
        var tran = conn.BeginTransaction();
        command.WithTransaction(tran);

        Assert.IsNotNull(command.Transaction);
    }

    [Test]
    public void WithTimeOutWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;
        var timeSpan = new TimeSpan(0, 0, 30);

        Assert.Throws<ArgumentNullException>(() => command.WithTimeout(timeSpan));
    }

    [Test]
    public void WithTimeOutSetsCommandTimeOut()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();
        var timeSpan = new TimeSpan(0, 0, 30);

        command.WithTimeout(timeSpan);

        Assert.AreEqual(30, command.CommandTimeout);
    }

    [Test]
    public void WithTimeOutSecondsWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.WithTimeout(30));
    }

    [Test]
    public void WithTimeOutSecondsSetsCommandTimeOut()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        command.WithTimeout(30);

        Assert.AreEqual(30, command.CommandTimeout);
    }

    [Test]
    public void WithCommandTypeWhenCommandIsNullThrowsException()
    {
        SqliteCommand? command = null;

        Assert.Throws<ArgumentNullException>(() => command.WithCommandType(CommandType.Text));
    }

    [Test]
    public void WithCommandTypeSetsCommandType()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        var command = conn.CreateCommand();

        command.WithCommandType(CommandType.Text);

        Assert.AreEqual(CommandType.Text, command.CommandType);
    }
}
