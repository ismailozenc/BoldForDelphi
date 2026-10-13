// Entry point of a test project in offline builds (BoldOffline=true); see tests/Directory.Build.props.
return new NUnitLite.AutoRun(System.Reflection.Assembly.GetExecutingAssembly()).Execute(args);
