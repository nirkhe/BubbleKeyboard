import java.util.ArrayList;


public class TrigramData implements Comparable
{

	public String name;
	
	public int assignednumber = -1;
	
	public TrigramData(String name)
	{
		this.name = name;
		numericalvalues = new ArrayList<Integer> ();
	}
	
	public ArrayList<Integer> numericalvalues;
	
	public boolean equals(Object arg0)
	{
		if (arg0.getClass() == this.getClass())
		{
			return this.name.equalsIgnoreCase(((TrigramData)(arg0)).name);			
		}
		return false;
	}
	

	@Override
	public int compareTo(Object arg0) 
	{
		if (arg0.getClass() == this.getClass())
		{
			return this.name.compareTo(((TrigramData)(arg0)).name);			
		}
		return -1;
	}
	
}
