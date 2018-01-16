int nextRect = 1; //<>//
float newX, newY;
float prevX, prevY;
float deg;
color bg = 255;
Rectangle[] recs;
void setup()
{
  size(1400, 800);
  rectMode(CENTER);
  recs = new Rectangle[50];
  recs[0] = new Rectangle(random(100, width-100), random(100, height-100), random(TWO_PI));
}

void draw()
{
  background(bg);
  for (Rectangle r : recs)
  {
    if (r != null) r.display();
  }
}

void keyPressed()
{
  prevX = recs[nextRect-1].x;
  prevY = recs[nextRect-1].y;
  float maxDist = 0; //maximum minimum distance to bricks for all pixels :)))
  for (int i=100; i<width-100; i+=10)
  {
    for (int j=100; j<height-100; j+=10)
    {
      float minDist = width;
      for (Rectangle r : recs)  if (r != null)
      {
        float d = sqrt( pow((r.x-i), 2) + pow((r.y-j), 2) );
        if (d<minDist) minDist = d;
      }

      if (minDist>maxDist)
      {
        maxDist = minDist;
        newX = i;
        newY = j;
      }
    }
  }

  if (maxDist<200) bg = color(255, 0, 0);
  deg = atan2(prevY-newY, prevX-newX);
  recs[nextRect] = new Rectangle(newX, newY, deg);
  nextRect++;
}