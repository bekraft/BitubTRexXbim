using Bitub.Xbim.Ifc.Tests;

using Xbim.Common.Configuration;

public class GeometryTestBase<T> : TestBase<T>
{
    protected GeometryTestBase() : base()
    {
        if (!XbimServices.Current.IsConfigured)
        {
            XbimServices.Current.ConfigureServices(opt => 
                opt.AddXbimToolkit(conf => 
                    conf.AddGeometryServices()
                ));
        }
    }
}