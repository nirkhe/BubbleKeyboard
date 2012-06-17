
public class Trigram implements Comparable
{
	public Trigram(TrigramData first, TrigramData second, TrigramData third, int frequency)
	{
		this.first = first;
		this.second = second;
		this.third = third;
		this.frequency = frequency;
		
	}
	
	public TrigramData first, second, third;
	public int frequency;
	
	@Override
	public int compareTo(Object arg0)
	{
		if (arg0.getClass() == this.getClass())
		{
			Trigram other = (Trigram)(arg0);
			int first_ = this.first.compareTo(other.first);
			if (first_ != 0)
			{
				return first_;
			}
			else
			{
				int second_ = this.second.compareTo(other.second);
				if (second_ != 0)
				{
					return second_;
				}
				else
				{
					int third_ = this.third.compareTo(other.third);
					if (third_ != 0)
					{
						return third_;
					}
					else
					{
						return (new Integer(this.frequency)).compareTo(new Integer(other.frequency));
					}
				}
			}
		}
		return 1;
	}

	
	
}
