int nextRect = 1;
Rectangle[] recs;
void setup()
{
  size(1400, 800);
  rectMode(CENTER);
  recs = new Rectangle[30];
  recs[0] = new Rectangle(280, 150, 0);
}

void draw()
{
  background(255);
  for (Rectangle r : recs)
  {
    if (r != null) r.display();
  }
  
}

void keyPressed()
{
  int newX = 0, newY = 0; //<>//
  float maxDist = 0; //maximum minimum distance to bricks for all pixels :)))
  float minDist;
  for (int i=100; i<width-100; i++)
  {
    for (int j=100; j<height-100; j++)
    {
      minDist = width;
      for (Rectangle r : recs)  if (r != null)
      {
        float d = sqrt( pow((r.x-i), 2) + pow((r.y-j), 2) );
        if (d<minDist) 
        {
          minDist = d;
        }
      }
      
      if (minDist>maxDist)
      {
        maxDist = minDist;
        newX = i;
        newY = j;
      }
      
    }
  }

  recs[nextRect] = new Rectangle(newX, newY, 0);
  nextRect++;
}