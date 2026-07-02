import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ResetareParola } from './resetare-parola';

describe('ResetareParola', () => {
  let component: ResetareParola;
  let fixture: ComponentFixture<ResetareParola>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResetareParola],
    }).compileComponents();

    fixture = TestBed.createComponent(ResetareParola);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
