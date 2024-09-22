namespace TeReo.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        string __(string s)
        {
            return s;
        }

        
        //var x= DateTime.Now;
 
        
        try
        {
            string str = __("Te reo Maori");
        }
        catch (Exception _)
        {
            
        }
        
        Assert.Pass();
    }
}