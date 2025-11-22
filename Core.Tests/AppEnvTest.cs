namespace Core.Tests
{
    public class AppEnvTest
    {
        [Fact]
        public void Env_WithDevEnvironmentVariable_ShouldReturnDev()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "dev");
            
            // Act
            var env = App.Env;
            
            // Assert
            Assert.Equal(AppEnv.Dev, env);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void Env_WithStagingEnvironmentVariable_ShouldReturnStaging()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "staging");
            
            // Act
            var env = App.Env;
            
            // Assert
            Assert.Equal(AppEnv.Staging, env);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void Env_WithProdEnvironmentVariable_ShouldReturnProd()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "prod");
            
            // Act
            var env = App.Env;
            
            // Assert
            Assert.Equal(AppEnv.Prod, env);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void Env_WithUpperCaseEnvironmentVariable_ShouldReturnCorrectValue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "PROD");
            
            // Act
            var env = App.Env;
            
            // Assert
            Assert.Equal(AppEnv.Prod, env);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void Env_WithNoEnvironmentVariable_ShouldReturnDev()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
            
            // Act
            var env = App.Env;
            
            // Assert
            Assert.Equal(AppEnv.Dev, env);
        }

        [Fact]
        public void Env_WithInvalidEnvironmentVariable_ShouldReturnDev()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "invalid");
            
            // Act
            var env = App.Env;
            
            // Assert
            Assert.Equal(AppEnv.Dev, env);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void GetSecret_WithExistingSecret_ShouldReturnValue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "dev");
            Environment.SetEnvironmentVariable("DEV_TEST_SECRET", "test_value");
            
            // Act
            var secret = App.GetSecret("TEST_SECRET");
            
            // Assert
            Assert.Equal("test_value", secret);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DEV_TEST_SECRET", null);
        }

        [Fact]
        public void GetSecret_WithMissingSecret_ShouldThrowException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "dev");
            Environment.SetEnvironmentVariable("DEV_MISSING_SECRET", null);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => App.GetSecret("MISSING_SECRET"));
            Assert.Contains("MISSING_SECRET", exception.Message);
            Assert.Contains("Dev", exception.Message);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void GetGlobalSecret_WithExistingSecret_ShouldReturnValue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GLOBAL_TEST_SECRET", "global_value");
            
            // Act
            var secret = App.GetGlobalSecret("GLOBAL_TEST_SECRET");
            
            // Assert
            Assert.Equal("global_value", secret);
            
            // Cleanup
            Environment.SetEnvironmentVariable("GLOBAL_TEST_SECRET", null);
        }

        [Fact]
        public void GetGlobalSecret_WithMissingSecret_ShouldThrowException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("MISSING_GLOBAL_SECRET", null);
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => App.GetGlobalSecret("MISSING_GLOBAL_SECRET"));
            Assert.Contains("MISSING_GLOBAL_SECRET", exception.Message);
            
            // Cleanup
        }

        [Fact]
        public void GetConfigFilename_ForDev_ShouldReturnDevFilename()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "dev");
            
            // Act
            var filename = App.GetConfigFilename("config");
            
            // Assert
            Assert.Equal("config.dev.json", filename);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void GetConfigFilename_ForStaging_ShouldReturnStagingFilename()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "staging");
            
            // Act
            var filename = App.GetConfigFilename("config");
            
            // Assert
            Assert.Equal("config.staging.json", filename);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void GetConfigFilename_ForProd_ShouldReturnProdFilename()
        {
            // Arrange
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", "prod");
            
            // Act
            var filename = App.GetConfigFilename("config");
            
            // Assert
            Assert.Equal("config.prod.json", filename);
            
            // Cleanup
            Environment.SetEnvironmentVariable("RUNTIME_ENVIRONMENT", null);
        }

        [Fact]
        public void GetConfigFilename_WithNullName_ShouldThrowException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => App.GetConfigFilename(null!));
            Assert.Contains("cannot be null or empty", exception.Message);
        }

        [Fact]
        public void GetConfigFilename_WithEmptyName_ShouldThrowException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => App.GetConfigFilename(string.Empty));
            Assert.Contains("cannot be null or empty", exception.Message);
        }

        [Fact]
        public void AppEnv_AllValues_ShouldExist()
        {
            // Assert - Verify all enum values exist
            Assert.Equal(AppEnv.Dev, AppEnv.Dev);
            Assert.Equal(AppEnv.Staging, AppEnv.Staging);
            Assert.Equal(AppEnv.Prod, AppEnv.Prod);
        }
    }
}
