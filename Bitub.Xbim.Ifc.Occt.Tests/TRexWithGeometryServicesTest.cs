using Xbim.Common.Configuration;

namespace Bitub.Xbim.Ifc.Tests;

public class TRexWithGeometryServicesTest<T> : TRexTest<T>
{
    protected override void BuildServiceProvider()
    {
        XbimServices.Current.ConfigureServices(opt =>
            opt.AddXbimToolkit(conf =>
                conf
                    .AddGeometryServices()
                    .AddMemoryModel()
            ));
    }
}