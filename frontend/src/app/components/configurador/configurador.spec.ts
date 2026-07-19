import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Configurador } from './configurador';

describe('Configurador', () => {
  let component: Configurador;
  let fixture: ComponentFixture<Configurador>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Configurador]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Configurador);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
