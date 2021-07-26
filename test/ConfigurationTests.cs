using System.Configuration;
using NUnit.Framework;
using uDateFoldersy.Configuration;
using uDateFoldersy.Helpers;

namespace uDateFoldersy.Tests
{
	public class ConfigurationTests
	{
		private uDateFoldersySettingsSection config;
		[SetUp]
		public void Setup()
		{
			config = ConfigurationManager.GetSection("uDateFoldersySettings") as uDateFoldersySettingsSection;
			var testConfig = ConfigReader.Instance;
		}

		[Test]
		public void Test_DefaultConfigurationLoads()
		{
			Assert.IsNotNull(config);
			Assert.True(config.DateFolders.Enabled);
			Assert.True(config.DateFolders.YearFolders.Enabled);
			Assert.AreEqual("uDateFoldersyFolderYear", config.DateFolders.YearFolders.DoctypeAlias);
			Assert.True(config.DateFolders.MonthFolders.Enabled);
			Assert.AreEqual("uDateFoldersyFolderMonth", config.DateFolders.MonthFolders.DoctypeAlias);
			Assert.True(config.DateFolders.DayFolders.Enabled);
			Assert.AreEqual("uDateFoldersyFolderDay", config.DateFolders.DayFolders.DoctypeAlias);
			Assert.AreEqual("uBlogsyPost", config.DateFolders.TargetDocTypeAliases.Value);
			Assert.AreEqual("uBlogsyPostDate", config.DateFolders.DatePropertyAlias.Value);
			Assert.AreEqual("dd", config.DateFolders.FolderNameFormat.DayFormat.Value);
			Assert.AreEqual("MMMM", config.DateFolders.FolderNameFormat.MonthFormat.Value);
		}
	}
}